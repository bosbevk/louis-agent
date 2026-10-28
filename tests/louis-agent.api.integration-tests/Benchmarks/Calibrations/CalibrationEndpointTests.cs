using LouisAgent.Testing;
using System.Net;
using System.Text.Json;
using LouisAgent.Api.Benchmarks.Calibrations;
using LouisAgent.Api.Benchmarks.Calibrations.Store;
using LouisAgent.Api.Benchmarks.Reviews;
using LouisAgent.Api.Benchmarks.Reviews.Store;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Api.IntegrationTests.Benchmarks.Runs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;

namespace LouisAgent.Api.IntegrationTests.Benchmarks.Calibrations;

/// <summary>
/// The judge-check endpoints. A POST under a run checks one judge against the people's grades of that run: it pipes
/// the decided grades to the eval runner as label lines and answers 202 with the run it started. It's a 404 for a run
/// the store doesn't hold or nobody graded for that judge, a 400 for a grader with no human review on the run or a run
/// of an eval the runner doesn't list, a 409 while a run runs, and a 503 without a store. A GET lists the reports
/// kept, newest first. The runner judges; the API only starts it, keeps its report and shows it.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0039-api-routes-under-v1.md">ADR 0039: the API's routes follow one rule, under /v1</seealso>
[Category("Integration")]
public class CalibrationEndpointTests
{
    /// <summary>The admin key the test API is built with.</summary>
    private const string AdminKey = "admin-secret";

    /// <summary>The stored run whose judge gets checked.</summary>
    private const string RunId = "76667d39dcdb";

    /// <summary>When the stored run started.</summary>
    private static readonly DateTimeOffset T0 = new(2026, 10, 26, 9, 0, 0, TimeSpan.Zero);

    /// <summary>The running API and what a test reaches past it: the stores, the runner and the recorder.</summary>
    private sealed record Host(
        WebApplication App,
        HttpClient Client,
        InMemoryBenchmarkStore Store,
        InMemoryHumanReviewStore Reviews,
        InMemoryCalibrationStore Calibrations,
        BenchmarkRunner Runner,
        CalibrationRecorder Recorder) : IAsyncDisposable
    {
        /// <summary>Stops the API when the test is done with it.</summary>
        public async ValueTask DisposeAsync() => await App.DisposeAsync();
    }

    /// <summary>
    /// Starts the API over the real runner, with one stored cook run of two conversations, its judge and the judge's
    /// human review, and Louis's grade of one conversation. A test can leave out the report store or the grade.
    /// </summary>
    private static async Task<Host> StartAsync(bool withCalibrations = true, bool graded = true)
    {
        // The stores, and the recorder that keeps the judge-check reports.
        var store = new InMemoryBenchmarkStore();
        var reviews = new InMemoryHumanReviewStore();
        var calibrations = new InMemoryCalibrationStore();
        var recorder = new CalibrationRecorder(calibrations);

        // The cook run, with a fixed rule, its judge and the judge's human review; and a run of an eval that's gone.
        await store.SaveRunAsync(new Run(RunId, SampleEval.Beta, 1, BenchmarkRunStatus.Done, T0, T0.AddMinutes(5), 0,
            Eval: SampleEval.Beta,
            Graders:
            [
                new Grader("cook-no-error", 1, BenchmarkGraderKind.FixedRule),
                new Grader("meal-for-people", 1, BenchmarkGraderKind.Model),
                new Grader("meal-for-people.human", 1, BenchmarkGraderKind.Human)
            ]));
        await store.SaveRunAsync(new Run("gone1234567", "gone-check", 1, BenchmarkRunStatus.Done, T0, T0.AddMinutes(5),
            0,
            Graders:
            [
                new Grader("meal-for-people", 1, BenchmarkGraderKind.Model),
                new Grader("meal-for-people.human", 1, BenchmarkGraderKind.Human)
            ]));

        // Two conversations in the cook run, each passed by the judge, with one turn each.
        foreach (var (task, answer) in new[]
                 {
                     (SampleScenario.Beta, "Bobotie for four"), (SampleScenario.Alpha, "Net 'n sousie")
                 })
        {
            var key = new BenchmarkProtocol.ConversationKey(SampleMode.Beta, 1, task);
            await store.SaveConversationAsync(RunId, new Conversation(key, 1, null,
                JsonDocument.Parse("{}").RootElement.Clone(), 0, 1.0,
                [new Grade("meal-for-people", 1, 1, BenchmarkOutcome.Pass, "the judge's reason")]));
            await store.SaveTurnAsync(RunId,
                new Turn(new BenchmarkProtocol.TurnKey(SampleMode.Beta, 1, task, 1), "sess_1", "For four", answer,
                    SampleRole.Alpha, null, 1, 1, [], 1.0, null), []);
        }

        // Louis's grade of the first conversation, for the judge to be checked against.
        if (graded)
            await reviews.AddAsync(new HumanGrade(RunId,
                new BenchmarkProtocol.ConversationKey(SampleMode.Beta, 1, SampleScenario.Beta), "meal-for-people",
                HumanVerdict.Pass, "a meal", "Louis", T0));

        // The real runner, knowing only the cook eval, and the API over it all.
        var catalog = new BenchmarkCatalog([new BenchmarkCheck(SampleEval.Beta, "Cook")]);
        var runner = new BenchmarkRunner(catalog, TestPaths.RepoRoot, "http://127.0.0.1:1/", RunnerBuild.Dll);
        var app = ApiTestHost.Build(adminKey: AdminKey, store: store, reviews: reviews, catalog: catalog,
            runner: runner,
            calibrations: withCalibrations ? calibrations : null,
            calibrationRecorder: withCalibrations ? recorder : null);
        await app.StartAsync();

        return new Host(app, app.GetTestClient(), store, reviews, calibrations, runner, recorder);
    }

    /// <summary>Sends a request with the admin key.</summary>
    private static Task<HttpResponseMessage> Send(HttpClient client, HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("x-api-key", AdminKey);
        return client.SendAsync(request);
    }

    /// <summary>The address of a run's judge checks, with an optional grader on the end.</summary>
    private static string Path(string run = RunId, string tail = "") =>
        $"/v1/admin/benchmarks/runs/{run}/calibrations{tail}";

    /// <summary>
    /// A POST starts the runner on the people's labels and answers 202 with the run; the report lands in the store
    /// when a judge could run, and otherwise the recorder says why not.
    /// </summary>
    [Test]
    public async Task APost_StartsTheRunnerOnThePeoplesLabels_AndAnswers202WithTheRun()
    {
        await using var host = await StartAsync();

        var response = await Send(host.Client, HttpMethod.Post, Path(tail: "/meal-for-people"));
        var row = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
        Assert.That(
            (row.GetProperty("check").GetString(), row.GetProperty("calibration_of").GetString(),
                row.GetProperty("grader").GetString()), Is.EqualTo((SampleEval.Beta, RunId, "meal-for-people")));
        string id = row.GetProperty("id").GetString()!;
        Assert.That(host.Runner.Current!.Id, Is.EqualTo(id), "the runner's one run at a time");
        Assert.That(host.Runner.Current.Input,
            Does.Contain("\"grader\":\"meal-for-people\"").And
                .Contain(SampleScenario.Fill("\"scenario\":\"<ScenarioBeta>\"")).And.Not.Contain(SampleScenario.Alpha),
            "one label line per decided grade, as the zip would hold them");
        Assert.That(host.Runner.Current.Input!.TrimEnd('\n'), Does.Not.Contain("\n"), "one line per label");

        // The real runner, on the real cook eval: on a machine with no judge set it judges "can't tell" and reports,
        // so the one label's report lands in the store, the proof that the line went in through its input and the
        // report came back. In CI the runner refuses (it never calibrates there), so nothing is stored and the result
        // says why.
        await foreach (string _ in host.Runner.EventsAsync(id))
        {
        }

        var result = await host.Recorder.RecordingOf(id)!;
        var reports = await host.Calibrations.OfRunAsync(RunId);
        if (result.Stored)
        {
            var report = reports.Single();
            Assert.That(
                (report.Run, report.Grader, report.Total,
                    report.PersonPass.JudgePass + report.PersonPass.JudgeFail + report.PersonPass.JudgeUnknown),
                Is.EqualTo((RunId, "meal-for-people", 1, 1)),
                "the one label Louis gave, judged");
            Assert.That(report.At, Is.EqualTo(host.Runner.Current.EndedAt));
        }
        else
        {
            Assert.That(result.Error, Is.Not.Null.And.Not.Empty);
            Assert.That(reports, Is.Empty);
        }
    }

    /// <summary>Checking a grader that has no human review on the run is a 400 that names the ones that do.</summary>
    [Test]
    public async Task AJudgeWithoutAHumanReviewOnTheRun_Is400_NamingTheOnesThatHave()
    {
        await using var host = await StartAsync();

        var response = await Send(host.Client, HttpMethod.Post, Path(tail: "/cook-no-error"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(await response.Content.ReadAsStringAsync(),
            Does.Contain("cook-no-error").And.Contain("meal-for-people"));
    }

    /// <summary>With no person's grades to check against, it's a 404 and nothing starts.</summary>
    [Test]
    public async Task ARunNobodyHasGradedForThatJudge_Is404()
    {
        await using var host = await StartAsync(graded: false);

        var response = await Send(host.Client, HttpMethod.Post, Path(tail: "/meal-for-people"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(await response.Content.ReadAsStringAsync(), Does.Contain("meal-for-people"));
        Assert.That(host.Runner.Current, Is.Null, "nothing was started");
    }

    /// <summary>A run of an eval the runner doesn't list is a 400; a run the store doesn't hold is a 404.</summary>
    [Test]
    public async Task ARunOfACheckTheRunnerDoesNotList_Is400_AndAnUnknownRun_Is404()
    {
        await using var host = await StartAsync();

        Assert.That((await Send(host.Client, HttpMethod.Post, Path("gone1234567", "/meal-for-people"))).StatusCode,
            Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That((await Send(host.Client, HttpMethod.Post, Path("nope00000000", "/meal-for-people"))).StatusCode,
            Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That((await Send(host.Client, HttpMethod.Get, Path("nope00000000"))).StatusCode,
            Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>Without a place to keep the reports, both the POST and the GET answer 503.</summary>
    [Test]
    public async Task WithoutACalibrationStore_Both_Are503()
    {
        await using var host = await StartAsync(withCalibrations: false);

        Assert.That((await Send(host.Client, HttpMethod.Post, Path(tail: "/meal-for-people"))).StatusCode,
            Is.EqualTo(HttpStatusCode.ServiceUnavailable));
        Assert.That((await Send(host.Client, HttpMethod.Get, Path())).StatusCode,
            Is.EqualTo(HttpStatusCode.ServiceUnavailable));
    }

    /// <summary>A GET lists the run's reports newest first, every field under its wire name.</summary>
    [Test]
    public async Task AGet_ListsTheRunsReports_NewestFirst_InThePortalsShape()
    {
        // Two reports on the run: an older one that fails the bar with one disagreement, a newer one that passes.
        await using var host = await StartAsync();
        await host.Calibrations.AddAsync(new CalibrationRecord(RunId, "meal-for-people", "qwen3:4b", T0, 35, 36, 90,
            false, new CalibrationClassCounts(27, 0, 0), new CalibrationClassCounts(1, 8, 0),
            [new CalibrationDisagreement("cook-shops", SampleMode.Beta, 2, "fail", "pass", "a dish with steps")]));
        await host.Calibrations.AddAsync(new CalibrationRecord(RunId, "meal-for-people", "qwen3:4b", T0.AddMinutes(10),
            36, 36, 90, true, new CalibrationClassCounts(27, 0, 0), new CalibrationClassCounts(0, 9, 0), []));

        var response = await Send(host.Client, HttpMethod.Get, Path());
        var list = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement
            .GetProperty("calibrations");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(list.EnumerateArray().Select(c => c.GetProperty("agreed").GetInt32()),
            Is.EqualTo(new[] { 36, 35 }));
        var older = list[1];
        Assert.That(
            (older.GetProperty("grader").GetString(), older.GetProperty("judge_model").GetString(),
                older.GetProperty("total").GetInt32(), older.GetProperty("bar").GetInt32(),
                older.GetProperty("passes").GetBoolean()),
            Is.EqualTo(("meal-for-people", "qwen3:4b", 36, 90, false)));
        Assert.That(older.GetProperty("at").GetDateTimeOffset(), Is.EqualTo(T0));
        Assert.That(older.GetProperty("person_fail").GetProperty("judge_pass").GetInt32(), Is.EqualTo(1));
        var d = older.GetProperty("disagreements").EnumerateArray().Single();
        Assert.That(
            (d.GetProperty("scenario").GetString(), d.GetProperty("mode").GetString(),
                d.GetProperty("repeat").GetInt32(), d.GetProperty("label").GetString(),
                d.GetProperty("judge").GetString(), d.GetProperty("reason").GetString()),
            Is.EqualTo(("cook-shops", SampleMode.Beta, 2, "fail", "pass", "a dish with steps")));
    }

    /// <summary>Deleting a run takes its judge-check reports with it.</summary>
    [Test]
    public async Task DeletingARun_DeletesItsReportsToo()
    {
        await using var host = await StartAsync();
        await host.Calibrations.AddAsync(new CalibrationRecord(RunId, "meal-for-people", null, T0, 36, 36, 90, true,
            new CalibrationClassCounts(27, 0, 0), new CalibrationClassCounts(0, 9, 0), []));

        var response = await Send(host.Client, HttpMethod.Delete, $"/v1/admin/benchmarks/runs/{RunId}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(await host.Calibrations.OfRunAsync(RunId), Is.Empty);
    }
}
