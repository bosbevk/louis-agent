using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LouisAgent.Api.Evals.Reviews;
using LouisAgent.Api.Evals.Reviews.Store;
using LouisAgent.Api.Evals.Runs.Kinds;
using LouisAgent.Api.Evals.Runs.Protocol;
using LouisAgent.Api.Evals.Store;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Tests.Evals.Reviews;

/// <summary>
/// People grading a stored run by hand. A PUT stores one person's verdict on one conversation for the run's human
/// review (such as <c>meal-for-people.human</c>), the list keeps the history, the run's own read shows the people's
/// grades beside the judge's and counts how often they agreed, and the labels export the passes and fails as a zip.
/// Bad verdicts, graders and reviewers are a 400; an unknown run or conversation is a 404.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0039-api-routes-under-v1.md">ADR 0039: API routes under v1</seealso>
public class ReviewEndpointTests
{
    private const string AdminKey = "admin-secret";
    private const string RunId = "8f7802b9a72e";
    private static readonly DateTimeOffset T0 = new(2026, 10, 26, 9, 0, 0, TimeSpan.Zero);

    /// <summary>The running API with its client and the two stores, so a test can check what was stored.</summary>
    private sealed record Host(
        WebApplication App,
        HttpClient Client,
        InMemoryEvalStore Store,
        InMemoryHumanReviewStore Reviews) : IAsyncDisposable
    {
        /// <summary>Stops the API when the test is done.</summary>
        public async ValueTask DisposeAsync() => await App.DisposeAsync();
    }

    /// <summary>
    /// Starts the API over one finished run with two conversations: the judge passed bobotie and failed sosaties. The
    /// run has a fixed-rule grader, a model grader and that model grader's human review.
    /// </summary>
    private static async Task<Host> StartAsync()
    {
        // The run, with its three graders.
        var store = new InMemoryEvalStore();
        var reviews = new InMemoryHumanReviewStore();
        await store.SaveRunAsync(new Run(RunId, SampleEval.Beta, 1, EvalRunStatus.Done, T0, T0.AddMinutes(5), 0,
            Graders:
            [
                new Grader("cook-no-error", 1, EvalGraderKind.FixedRule),
                new Grader("meal-for-people", 1, EvalGraderKind.Model),
                new Grader("meal-for-people.human", 1, EvalGraderKind.Human)
            ]));

        // Each conversation with the judge's grade and its one turn.
        foreach (var (task, outcome, answer) in new[]
                 {
                     ("bobotie", EvalOutcome.Pass, "Bobotie vir vier"),
                     ("sosaties", EvalOutcome.Fail, "Net 'n sousie")
                 })
        {
            await store.SaveConversationAsync(RunId, new Conversation(
                new EvalProtocol.ConversationKey(SampleMode.Beta, 1, task), 1, null,
                JsonDocument.Parse("{}").RootElement.Clone(), 0, 1.0,
                [
                    new Grade("meal-for-people", 1, outcome == EvalOutcome.Pass ? 1 : 0, outcome,
                        "the judge's reason")
                ]));
            await store.SaveTurnAsync(RunId,
                new Turn(new EvalProtocol.TurnKey(SampleMode.Beta, 1, task, 1), "sess_1", "Vraag", answer,
                    SampleRole.Alpha, null, 1, 1, [], 1.0, null), []);
        }

        var app = ApiTestHost.Build(adminKey: AdminKey, store: store, reviews: reviews);
        await app.StartAsync();
        return new Host(app, app.GetTestClient(), store, reviews);
    }

    /// <summary>The address of this run's reviews, with whatever follows it.</summary>
    private static string Path(string tail = "") => $"/v1/admin/benchmarks/runs/{RunId}/reviews{tail}";

    /// <summary>Sends a request, with the admin key unless the test leaves it off, and any body as JSON.</summary>
    private static Task<HttpResponseMessage> Send(HttpClient client, HttpMethod method, string url, object? body = null,
        bool withKey = true)
    {
        var request = new HttpRequestMessage(method, url);
        if (withKey) request.Headers.Add("x-api-key", AdminKey);
        if (body is not null) request.Content = JsonContent.Create(body);
        return client.SendAsync(request);
    }

    /// <summary>A person's grade as the page sends it; each test changes only the part it is about.</summary>
    private static object Body(string verdict = "pass", string note = "a main dish", string reviewer = "Louis") =>
        new
        {
            verdict,
            note,
            reviewer
        };

    /// <summary>Reads an answer's body as JSON.</summary>
    private static async Task<JsonElement> JsonOf(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();

    /// <summary>
    /// A grade is stored for the model grader it answers, with who gave it and when, and the answer hands it back.
    /// </summary>
    [Test]
    public async Task APut_StoresTheGrade_WithWhoAndWhen_AndAnswers200WithIt()
    {
        await using var host = await StartAsync();

        var response = await Send(host.Client, HttpMethod.Put,
            Path($"/{SampleMode.Beta}/1/bobotie/meal-for-people.human"), Body("fail", "a side", "Louis"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var row = await JsonOf(response);
        Assert.That(
            (row.GetProperty("verdict").GetString(), row.GetProperty("reviewer").GetString(),
                row.GetProperty("note").GetString()), Is.EqualTo(("fail", "Louis", "a side")));
        var stored = (await host.Reviews.OfRunAsync(RunId)).Single();
        Assert.That((stored.Grader, stored.Verdict, stored.Conversation.Task),
            Is.EqualTo(("meal-for-people", HumanVerdict.Fail, "bobotie")), "stored for the model grader it answers");
        Assert.That(stored.At, Is.EqualTo(DateTimeOffset.UtcNow).Within(TimeSpan.FromMinutes(1)));
    }

    /// <summary>Grading again keeps the earlier grade, and the list marks which one is the latest.</summary>
    [Test]
    public async Task ASecondPut_KeepsTheFirst_AndTheListMarksTheLatest()
    {
        // Two grades of the same conversation, a moment apart so their times differ.
        await using var host = await StartAsync();
        await Send(host.Client, HttpMethod.Put, Path($"/{SampleMode.Beta}/1/bobotie/meal-for-people.human"),
            Body("fail"));
        await Task.Delay(20);
        await Send(host.Client, HttpMethod.Put, Path($"/{SampleMode.Beta}/1/bobotie/meal-for-people.human"),
            Body("pass"));

        var listed = await JsonOf(await Send(host.Client, HttpMethod.Get, Path()));

        var rows = listed.GetProperty("reviews").EnumerateArray().ToList();
        Assert.That(rows.Select(r => (r.GetProperty("verdict").GetString(), r.GetProperty("latest").GetBoolean())),
            Is.EqualTo(new[] { ("fail", false), ("pass", true) }));
        Assert.That(rows[0].GetProperty("grader").GetString(), Is.EqualTo("meal-for-people.human"));
        Assert.That(rows[0].GetProperty("model_grader").GetString(), Is.EqualTo("meal-for-people"));
    }

    /// <summary>A verdict that isn't pass, fail or unsure is refused, and nothing is stored.</summary>
    [TestCase("maybe")]
    [TestCase("")]
    [TestCase("1")]
    public async Task AnUnknownVerdict_Is400(string verdict)
    {
        await using var host = await StartAsync();

        var response = await Send(host.Client, HttpMethod.Put,
            Path($"/{SampleMode.Beta}/1/bobotie/meal-for-people.human"), Body(verdict));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(await host.Reviews.OfRunAsync(RunId), Is.Empty);
    }

    /// <summary>People grade only through one of the run's human reviews, never a model or fixed-rule grader.</summary>
    [TestCase("meal-for-people")]
    [TestCase("cook-no-error")]
    [TestCase("no-such.human")]
    public async Task AGraderThatIsNotAHumanReviewOfTheRun_Is400(string grader)
    {
        await using var host = await StartAsync();

        var response = await Send(host.Client, HttpMethod.Put, Path($"/{SampleMode.Beta}/1/bobotie/{grader}"), Body());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(await host.Reviews.OfRunAsync(RunId), Is.Empty);
    }

    /// <summary>
    /// A note is capped, so one request can't fill the store with a single grade; the answer names the limit and
    /// nothing is stored.
    /// </summary>
    [Test]
    public async Task ANoteOver2000Characters_Is400_NamingTheLimit_AndNothingIsStored()
    {
        await using var host = await StartAsync();

        var response = await Send(host.Client, HttpMethod.Put,
            Path($"/{SampleMode.Beta}/1/bobotie/meal-for-people.human"), Body(note: new string('x', 2001)));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That((await response.Content.ReadAsStringAsync()), Does.Contain("2000"));
        Assert.That(await host.Reviews.OfRunAsync(RunId), Is.Empty);
    }

    /// <summary>A reviewer's name is capped the same way, and the answer names that limit.</summary>
    [Test]
    public async Task AReviewerNameOver100Characters_Is400_NamingTheLimit_AndNothingIsStored()
    {
        await using var host = await StartAsync();

        var response = await Send(host.Client, HttpMethod.Put,
            Path($"/{SampleMode.Beta}/1/bobotie/meal-for-people.human"), Body(reviewer: new string('L', 101)));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That((await response.Content.ReadAsStringAsync()), Does.Contain("100"));
        Assert.That(await host.Reviews.OfRunAsync(RunId), Is.Empty);
    }

    /// <summary>A note and a name right at their limits still go through.</summary>
    [Test]
    public async Task ANoteOfExactly2000_AndANameOfExactly100_AreAccepted()
    {
        await using var host = await StartAsync();

        var response = await Send(host.Client, HttpMethod.Put,
            Path($"/{SampleMode.Beta}/1/bobotie/meal-for-people.human"),
            Body(note: new string('x', 2000), reviewer: new string('L', 100)));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    /// <summary>Every grade needs someone's name on it.</summary>
    [TestCase("")]
    [TestCase("   ")]
    public async Task ABlankReviewer_Is400(string reviewer)
    {
        await using var host = await StartAsync();

        var response = await Send(host.Client, HttpMethod.Put,
            Path($"/{SampleMode.Beta}/1/bobotie/meal-for-people.human"), Body(reviewer: reviewer));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    /// <summary>
    /// A run we don't have, or a conversation the run doesn't have, is not found, for grading and for listing.
    /// </summary>
    [Test]
    public async Task AnUnknownRun_Is404_AndSoIsAConversationTheRunDoesNotHave()
    {
        await using var host = await StartAsync();

        var noRun = await Send(host.Client, HttpMethod.Put,
            $"/v1/admin/benchmarks/runs/nope/reviews/{SampleMode.Beta}/1/bobotie/meal-for-people.human", Body());
        var noConversation = await Send(host.Client, HttpMethod.Put,
            Path($"/{SampleMode.Beta}/1/zzz/meal-for-people.human"), Body());
        var noRunList = await Send(host.Client, HttpMethod.Get, "/v1/admin/benchmarks/runs/nope/reviews");

        Assert.That((noRun.StatusCode, noConversation.StatusCode, noRunList.StatusCode),
            Is.EqualTo((HttpStatusCode.NotFound, HttpStatusCode.NotFound, HttpStatusCode.NotFound)));
    }

    /// <summary>Without the admin key nobody gets to grade, and nothing is stored.</summary>
    [Test]
    public async Task WithNoAdminKey_NobodyGradesAnything()
    {
        await using var host = await StartAsync();

        var response = await Send(host.Client, HttpMethod.Put,
            Path($"/{SampleMode.Beta}/1/bobotie/meal-for-people.human"), Body(), withKey: false);

        Assert.That(response.StatusCode, Is.AnyOf(HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized));
        Assert.That(await host.Reviews.OfRunAsync(RunId), Is.Empty);
    }

    /// <summary>
    /// Reading the run shows a person's grade beside the judge's, and counts per model grader how often the person
    /// and the judge agreed.
    /// </summary>
    [Test]
    public async Task TheRunsOwnRead_PutsTheHumanGradesBesideTheOthers_AndCountsTheAgreement()
    {
        // The person passes both; the judge passed only bobotie.
        await using var host = await StartAsync();
        await Send(host.Client, HttpMethod.Put, Path($"/{SampleMode.Beta}/1/bobotie/meal-for-people.human"),
            Body("pass"));
        await Send(host.Client, HttpMethod.Put, Path($"/{SampleMode.Beta}/1/sosaties/meal-for-people.human"),
            Body("pass"));

        var run = await JsonOf(await Send(host.Client, HttpMethod.Get, $"/v1/admin/benchmarks/runs/{RunId}"));

        var bobotie = run.GetProperty("conversations").EnumerateArray()
            .Single(c => c.GetProperty("task").GetString() == "bobotie");
        Assert.That(bobotie.GetProperty("grades").GetArrayLength(), Is.EqualTo(1), "the judge's grade is still there");
        var human = bobotie.GetProperty("human_grades").EnumerateArray().Single();
        Assert.That((human.GetProperty("verdict").GetString(), human.GetProperty("reviewer").GetString()),
            Is.EqualTo(("pass", "Louis")));

        var agreement = run.GetProperty("agreement").EnumerateArray().Single();
        Assert.That(agreement.GetProperty("grader").GetString(), Is.EqualTo("meal-for-people"));
        Assert.That(agreement.GetProperty("person_pass").GetProperty("judge_pass").GetInt32(), Is.EqualTo(1),
            "bobotie: the person and the judge both passed");
        Assert.That(agreement.GetProperty("person_pass").GetProperty("judge_fail").GetInt32(), Is.EqualTo(1),
            "sosaties: the person passed, the judge failed");
        Assert.That(agreement.GetProperty("person_fail").GetProperty("judge_pass").GetInt32(), Is.Zero);
    }

    /// <summary>
    /// A run nobody has graded yet still has its people's grades and agreement in the answer, empty and zero.
    /// </summary>
    [Test]
    public async Task ARunWithNoPeopleGrades_HasEmptyHumanGradesAndAZeroAgreement_NeverMissing()
    {
        await using var host = await StartAsync();

        var run = await JsonOf(await Send(host.Client, HttpMethod.Get, $"/v1/admin/benchmarks/runs/{RunId}"));

        Assert.That(run.GetProperty("conversations")[0].GetProperty("human_grades").GetArrayLength(), Is.Zero);
        Assert.That(
            run.GetProperty("agreement").EnumerateArray().Single().GetProperty("person_pass").GetProperty("judge_pass")
                .GetInt32(), Is.Zero);
    }

    /// <summary>
    /// The labels come as a zip with one file per pass or fail, holding the conversation's turns and the person's
    /// verdict; an unsure is left out.
    /// </summary>
    [Test]
    public async Task TheLabels_AreAZipOfOneFilePerPassOrFail_NamedForTheirAnswer_AndNeverAnUnsure()
    {
        await using var host = await StartAsync();
        await Send(host.Client, HttpMethod.Put, Path($"/{SampleMode.Beta}/1/bobotie/meal-for-people.human"),
            Body("pass", "a main dish"));
        await Send(host.Client, HttpMethod.Put, Path($"/{SampleMode.Beta}/1/sosaties/meal-for-people.human"),
            Body("unsure", "cannot tell"));

        var response = await Send(host.Client, HttpMethod.Get,
            $"/v1/admin/benchmarks/runs/{RunId}/labels?grader=meal-for-people");

        // One file in the zip: the unsure isn't a label.
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/zip"));
        using var zip = new ZipArchive(await response.Content.ReadAsStreamAsync());
        Assert.That(zip.Entries.Select(e => e.FullName),
            Is.EqualTo(new[] { $"meal-for-people.bobotie.{SampleMode.Beta}.{RunId}.1.json" }),
            "the unsure is not a label");

        // Its content: the grader, the turns, and the person's verdict with the note.
        var label = JsonDocument.Parse(new StreamReader(zip.Entries.Single().Open()).ReadToEnd()).RootElement;
        Assert.That(label.GetProperty("grader").GetString(), Is.EqualTo("meal-for-people"));
        var turns = label.GetProperty("turns").EnumerateArray()
            .Select(t => (t.GetProperty("question").GetString(), t.GetProperty("answer").GetString())).ToList();
        Assert.That((label.GetProperty("scenario").GetString(), turns),
            Is.EqualTo(("bobotie", new List<(string?, string?)> { ("Vraag", "Bobotie vir vier") })),
            "6a.4f: the turns, question and answer");
        Assert.That(
            (label.GetProperty("label").GetProperty("pass").GetBoolean(),
                label.GetProperty("label").GetProperty("note").GetString()), Is.EqualTo((true, "a main dish")));
    }

    /// <summary>When a person changes their mind, the label carries their latest grade, not the earlier one.</summary>
    [Test]
    public async Task TheLabelsOfAPersonsLatestGrade_AreWhatIsExported_NotTheEarlierOne()
    {
        await using var host = await StartAsync();
        await Send(host.Client, HttpMethod.Put, Path($"/{SampleMode.Beta}/1/bobotie/meal-for-people.human"),
            Body("pass"));
        await Task.Delay(20);
        await Send(host.Client, HttpMethod.Put, Path($"/{SampleMode.Beta}/1/bobotie/meal-for-people.human"),
            Body("fail", "changed my mind"));

        var response = await Send(host.Client, HttpMethod.Get,
            $"/v1/admin/benchmarks/runs/{RunId}/labels?grader=meal-for-people");

        using var zip = new ZipArchive(await response.Content.ReadAsStreamAsync());
        var label = JsonDocument.Parse(new StreamReader(zip.Entries.Single().Open()).ReadToEnd()).RootElement
            .GetProperty("label");
        Assert.That((label.GetProperty("pass").GetBoolean(), label.GetProperty("note").GetString()),
            Is.EqualTo((false, "changed my mind")));
    }

    /// <summary>
    /// Labels need a grader the run has a human review for, or it's a 400. Nothing to export yet, or no such run, is
    /// a 404.
    /// </summary>
    [Test]
    public async Task TheLabels_RefuseAGraderTheRunHasNoReviewFor_AndSayWhenThereAreNone()
    {
        await using var host = await StartAsync();

        var noParam = await Send(host.Client, HttpMethod.Get, $"/v1/admin/benchmarks/runs/{RunId}/labels");
        var wrongGrader = await Send(host.Client, HttpMethod.Get,
            $"/v1/admin/benchmarks/runs/{RunId}/labels?grader=cook-no-error");
        var none = await Send(host.Client, HttpMethod.Get,
            $"/v1/admin/benchmarks/runs/{RunId}/labels?grader=meal-for-people");
        var noRun = await Send(host.Client, HttpMethod.Get,
            "/v1/admin/benchmarks/runs/nope/labels?grader=meal-for-people");

        Assert.That((noParam.StatusCode, wrongGrader.StatusCode, none.StatusCode, noRun.StatusCode),
            Is.EqualTo((HttpStatusCode.BadRequest, HttpStatusCode.BadRequest, HttpStatusCode.NotFound,
                HttpStatusCode.NotFound)));
    }

    /// <summary>Deleting a run takes its people's grades with it.</summary>
    [Test]
    public async Task DeletingTheRun_DeletesItsReviewsToo()
    {
        await using var host = await StartAsync();
        await Send(host.Client, HttpMethod.Put, Path($"/{SampleMode.Beta}/1/bobotie/meal-for-people.human"), Body());

        var deleted = await Send(host.Client, HttpMethod.Delete, $"/v1/admin/benchmarks/runs/{RunId}");

        Assert.That(deleted.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(await host.Reviews.OfRunAsync(RunId), Is.Empty);
    }
}
