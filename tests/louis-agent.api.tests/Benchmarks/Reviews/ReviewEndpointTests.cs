using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LouisAgent.Api.Benchmarks.Reviews;
using LouisAgent.Api.Benchmarks.Reviews.Store;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Api.Tests.Endpoints.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;

namespace LouisAgent.Api.Tests.Benchmarks.Reviews;

// F18 step 6a.2 (ADR 0039): people's grades of a stored run, under /v1/admin/benchmarks/runs/{id}/reviews. PUT .../reviews/{mode}/{repeat}/{task}/{grader} stores one (the grader is the
// run's human review, `meal-for-people.human`); GET .../reviews lists the run's, history included; the run's own read puts each conversation's human grades beside its other grades and
// counts, per model grader, how often the judge agreed with the people; GET .../labels?grader= exports the people's passes and fails as label files in a zip. 400 for an unknown verdict,
// a grader that is not a human review of that run, or a blank reviewer; 404 for an unknown run or conversation. Written first: none of it existed.
public class ReviewEndpointTests
{
    private const string AdminKey = "admin-secret";
    private const string RunId = "8f7802b9a72e";
    private static readonly DateTimeOffset T0 = new(2026, 10, 26, 9, 0, 0, TimeSpan.Zero);

    private sealed record Host(WebApplication App, HttpClient Client, InMemoryBenchmarkStore Store, InMemoryHumanReviewStore Reviews) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync() => await App.DisposeAsync();
    }

    /// <summary>The API over a store holding one run with two conversations of one scenario: the judge passed one and failed the other; a model grader and its human review are the run's.</summary>
    private static async Task<Host> StartAsync()
    {
        var store = new InMemoryBenchmarkStore();
        var reviews = new InMemoryHumanReviewStore();
        await store.SaveRunAsync(new Run(RunId, "puls-cook", 1, BenchmarkRunStatus.Done, T0, T0.AddMinutes(5), 0,
            Graders: [new Grader("cook-no-error", 1, BenchmarkGraderKind.FixedRule), new Grader("meal-for-people", 1, BenchmarkGraderKind.Model),
                      new Grader("meal-for-people.human", 1, BenchmarkGraderKind.Human)]));
        foreach (var (task, outcome, answer) in new[] { ("bobotie", BenchmarkOutcome.Pass, "Bobotie vir vier"), ("sosaties", BenchmarkOutcome.Fail, "Net 'n sousie") })
        {
            await store.SaveConversationAsync(RunId, new Conversation(new BenchmarkProtocol.ConversationKey("puls-cook", 1, task), 1, null,
                JsonDocument.Parse("{}").RootElement.Clone(), 0, 1.0, [new Grade("meal-for-people", 1, outcome == BenchmarkOutcome.Pass ? 1 : 0, outcome, "the judge's reason")]));
            await store.SaveTurnAsync(RunId, new Turn(new BenchmarkProtocol.TurnKey("puls-cook", 1, task, 1), "sess_1", "Vraag", answer, "puls-cook", null, 1, 1, [], 1.0, null), []);
        }
        var app = ApiTestHost.Build(adminKey: AdminKey, store: store, reviews: reviews);
        await app.StartAsync();
        return new Host(app, app.GetTestClient(), store, reviews);
    }

    private static string Path(string tail = "") => $"/v1/admin/benchmarks/runs/{RunId}/reviews{tail}";

    private static Task<HttpResponseMessage> Send(HttpClient client, HttpMethod method, string url, object? body = null, bool withKey = true)
    {
        var request = new HttpRequestMessage(method, url);
        if (withKey) request.Headers.Add("x-api-key", AdminKey);
        if (body is not null) request.Content = JsonContent.Create(body);
        return client.SendAsync(request);
    }

    private static object Body(string verdict = "pass", string note = "a main dish", string reviewer = "Louis") => new { verdict, note, reviewer };

    private static async Task<JsonElement> JsonOf(HttpResponseMessage response) => JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();

    [Test]
    public async Task APut_StoresTheGrade_WithWhoAndWhen_AndAnswers200WithIt()
    {
        await using var host = await StartAsync();

        var response = await Send(host.Client, HttpMethod.Put, Path("/puls-cook/1/bobotie/meal-for-people.human"), Body("fail", "a side", "Louis"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var row = await JsonOf(response);
        Assert.That((row.GetProperty("verdict").GetString(), row.GetProperty("reviewer").GetString(), row.GetProperty("note").GetString()), Is.EqualTo(("fail", "Louis", "a side")));
        var stored = (await host.Reviews.OfRunAsync(RunId)).Single();
        Assert.That((stored.Grader, stored.Verdict, stored.Conversation.Task), Is.EqualTo(("meal-for-people", HumanVerdict.Fail, "bobotie")), "stored for the model grader it answers");
        Assert.That(stored.At, Is.EqualTo(DateTimeOffset.UtcNow).Within(TimeSpan.FromMinutes(1)));
    }

    [Test]
    public async Task ASecondPut_KeepsTheFirst_AndTheListMarksTheLatest()
    {
        await using var host = await StartAsync();
        await Send(host.Client, HttpMethod.Put, Path("/puls-cook/1/bobotie/meal-for-people.human"), Body("fail"));
        await Task.Delay(20);
        await Send(host.Client, HttpMethod.Put, Path("/puls-cook/1/bobotie/meal-for-people.human"), Body("pass"));

        var listed = await JsonOf(await Send(host.Client, HttpMethod.Get, Path()));

        var rows = listed.GetProperty("reviews").EnumerateArray().ToList();
        Assert.That(rows.Select(r => (r.GetProperty("verdict").GetString(), r.GetProperty("latest").GetBoolean())), Is.EqualTo(new[] { ("fail", false), ("pass", true) }));
        Assert.That(rows[0].GetProperty("grader").GetString(), Is.EqualTo("meal-for-people.human"));
        Assert.That(rows[0].GetProperty("model_grader").GetString(), Is.EqualTo("meal-for-people"));
    }

    [TestCase("maybe")]
    [TestCase("")]
    [TestCase("1")]
    public async Task AnUnknownVerdict_Is400(string verdict)
    {
        await using var host = await StartAsync();

        var response = await Send(host.Client, HttpMethod.Put, Path("/puls-cook/1/bobotie/meal-for-people.human"), Body(verdict));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(await host.Reviews.OfRunAsync(RunId), Is.Empty);
    }

    [TestCase("meal-for-people")]
    [TestCase("cook-no-error")]
    [TestCase("no-such.human")]
    public async Task AGraderThatIsNotAHumanReviewOfTheRun_Is400(string grader)
    {
        await using var host = await StartAsync();

        var response = await Send(host.Client, HttpMethod.Put, Path($"/puls-cook/1/bobotie/{grader}"), Body());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(await host.Reviews.OfRunAsync(RunId), Is.Empty);
    }

    // F18 step 6a.2b (the review's nit): a note and a name are bounded, so a request cannot fill the store with one grade. The limit is named in the answer, and nothing is stored.
    [Test]
    public async Task ANoteOver2000Characters_Is400_NamingTheLimit_AndNothingIsStored()
    {
        await using var host = await StartAsync();

        var response = await Send(host.Client, HttpMethod.Put, Path("/puls-cook/1/bobotie/meal-for-people.human"), Body(note: new string('x', 2001)));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That((await response.Content.ReadAsStringAsync()), Does.Contain("2000"));
        Assert.That(await host.Reviews.OfRunAsync(RunId), Is.Empty);
    }

    [Test]
    public async Task AReviewerNameOver100Characters_Is400_NamingTheLimit_AndNothingIsStored()
    {
        await using var host = await StartAsync();

        var response = await Send(host.Client, HttpMethod.Put, Path("/puls-cook/1/bobotie/meal-for-people.human"), Body(reviewer: new string('L', 101)));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That((await response.Content.ReadAsStringAsync()), Does.Contain("100"));
        Assert.That(await host.Reviews.OfRunAsync(RunId), Is.Empty);
    }

    [Test]
    public async Task ANoteOfExactly2000_AndANameOfExactly100_AreAccepted()
    {
        await using var host = await StartAsync();

        var response = await Send(host.Client, HttpMethod.Put, Path("/puls-cook/1/bobotie/meal-for-people.human"), Body(note: new string('x', 2000), reviewer: new string('L', 100)));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [TestCase("")]
    [TestCase("   ")]
    public async Task ABlankReviewer_Is400(string reviewer)
    {
        await using var host = await StartAsync();

        var response = await Send(host.Client, HttpMethod.Put, Path("/puls-cook/1/bobotie/meal-for-people.human"), Body(reviewer: reviewer));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task AnUnknownRun_Is404_AndSoIsAConversationTheRunDoesNotHave()
    {
        await using var host = await StartAsync();

        var noRun = await Send(host.Client, HttpMethod.Put, "/v1/admin/benchmarks/runs/nope/reviews/puls-cook/1/bobotie/meal-for-people.human", Body());
        var noConversation = await Send(host.Client, HttpMethod.Put, Path("/puls-cook/1/zzz/meal-for-people.human"), Body());
        var noRunList = await Send(host.Client, HttpMethod.Get, "/v1/admin/benchmarks/runs/nope/reviews");

        Assert.That((noRun.StatusCode, noConversation.StatusCode, noRunList.StatusCode), Is.EqualTo((HttpStatusCode.NotFound, HttpStatusCode.NotFound, HttpStatusCode.NotFound)));
    }

    [Test]
    public async Task WithNoAdminKey_NobodyGradesAnything()
    {
        await using var host = await StartAsync();

        var response = await Send(host.Client, HttpMethod.Put, Path("/puls-cook/1/bobotie/meal-for-people.human"), Body(), withKey: false);

        Assert.That(response.StatusCode, Is.AnyOf(HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized));
        Assert.That(await host.Reviews.OfRunAsync(RunId), Is.Empty);
    }

    [Test]
    public async Task TheRunsOwnRead_PutsTheHumanGradesBesideTheOthers_AndCountsTheAgreement()
    {
        await using var host = await StartAsync();
        await Send(host.Client, HttpMethod.Put, Path("/puls-cook/1/bobotie/meal-for-people.human"), Body("pass"));
        await Send(host.Client, HttpMethod.Put, Path("/puls-cook/1/sosaties/meal-for-people.human"), Body("pass"));

        var run = await JsonOf(await Send(host.Client, HttpMethod.Get, $"/v1/admin/benchmarks/runs/{RunId}"));

        var bobotie = run.GetProperty("conversations").EnumerateArray().Single(c => c.GetProperty("task").GetString() == "bobotie");
        Assert.That(bobotie.GetProperty("grades").GetArrayLength(), Is.EqualTo(1), "the judge's grade is still there");
        var human = bobotie.GetProperty("human_grades").EnumerateArray().Single();
        Assert.That((human.GetProperty("verdict").GetString(), human.GetProperty("reviewer").GetString()), Is.EqualTo(("pass", "Louis")));
        var agreement = run.GetProperty("agreement").EnumerateArray().Single();
        Assert.That(agreement.GetProperty("grader").GetString(), Is.EqualTo("meal-for-people"));
        Assert.That(agreement.GetProperty("person_pass").GetProperty("judge_pass").GetInt32(), Is.EqualTo(1), "bobotie: the person and the judge both passed");
        Assert.That(agreement.GetProperty("person_pass").GetProperty("judge_fail").GetInt32(), Is.EqualTo(1), "sosaties: the person passed, the judge failed");
        Assert.That(agreement.GetProperty("person_fail").GetProperty("judge_pass").GetInt32(), Is.Zero);
    }

    [Test]
    public async Task ARunWithNoPeopleGrades_HasEmptyHumanGradesAndAZeroAgreement_NeverMissing()
    {
        await using var host = await StartAsync();

        var run = await JsonOf(await Send(host.Client, HttpMethod.Get, $"/v1/admin/benchmarks/runs/{RunId}"));

        Assert.That(run.GetProperty("conversations")[0].GetProperty("human_grades").GetArrayLength(), Is.Zero);
        Assert.That(run.GetProperty("agreement").EnumerateArray().Single().GetProperty("person_pass").GetProperty("judge_pass").GetInt32(), Is.Zero);
    }

    [Test]
    public async Task TheLabels_AreAZipOfOneFilePerPassOrFail_NamedForTheirAnswer_AndNeverAnUnsure()
    {
        await using var host = await StartAsync();
        await Send(host.Client, HttpMethod.Put, Path("/puls-cook/1/bobotie/meal-for-people.human"), Body("pass", "a main dish"));
        await Send(host.Client, HttpMethod.Put, Path("/puls-cook/1/sosaties/meal-for-people.human"), Body("unsure", "cannot tell"));

        var response = await Send(host.Client, HttpMethod.Get, $"/v1/admin/benchmarks/runs/{RunId}/labels?grader=meal-for-people");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/zip"));
        using var zip = new ZipArchive(await response.Content.ReadAsStreamAsync());
        Assert.That(zip.Entries.Select(e => e.FullName), Is.EqualTo(new[] { $"meal-for-people.bobotie.puls-cook.{RunId}.1.json" }), "the unsure is not a label");
        var label = JsonDocument.Parse(new StreamReader(zip.Entries.Single().Open()).ReadToEnd()).RootElement;
        Assert.That(label.GetProperty("grader").GetString(), Is.EqualTo("meal-for-people"));
        Assert.That((label.GetProperty("scenario").GetString(), label.GetProperty("turn").GetInt32(), label.GetProperty("answer").GetString()), Is.EqualTo(("bobotie", 1, "Bobotie vir vier")));
        Assert.That((label.GetProperty("label").GetProperty("pass").GetBoolean(), label.GetProperty("label").GetProperty("note").GetString()), Is.EqualTo((true, "a main dish")));
    }

    [Test]
    public async Task TheLabelsOfAPersonsLatestGrade_AreWhatIsExported_NotTheEarlierOne()
    {
        await using var host = await StartAsync();
        await Send(host.Client, HttpMethod.Put, Path("/puls-cook/1/bobotie/meal-for-people.human"), Body("pass"));
        await Task.Delay(20);
        await Send(host.Client, HttpMethod.Put, Path("/puls-cook/1/bobotie/meal-for-people.human"), Body("fail", "changed my mind"));

        var response = await Send(host.Client, HttpMethod.Get, $"/v1/admin/benchmarks/runs/{RunId}/labels?grader=meal-for-people");

        using var zip = new ZipArchive(await response.Content.ReadAsStreamAsync());
        var label = JsonDocument.Parse(new StreamReader(zip.Entries.Single().Open()).ReadToEnd()).RootElement.GetProperty("label");
        Assert.That((label.GetProperty("pass").GetBoolean(), label.GetProperty("note").GetString()), Is.EqualTo((false, "changed my mind")));
    }

    [Test]
    public async Task TheLabels_RefuseAGraderTheRunHasNoReviewFor_AndSayWhenThereAreNone()
    {
        await using var host = await StartAsync();

        var noParam = await Send(host.Client, HttpMethod.Get, $"/v1/admin/benchmarks/runs/{RunId}/labels");
        var wrongGrader = await Send(host.Client, HttpMethod.Get, $"/v1/admin/benchmarks/runs/{RunId}/labels?grader=cook-no-error");
        var none = await Send(host.Client, HttpMethod.Get, $"/v1/admin/benchmarks/runs/{RunId}/labels?grader=meal-for-people");
        var noRun = await Send(host.Client, HttpMethod.Get, "/v1/admin/benchmarks/runs/nope/labels?grader=meal-for-people");

        Assert.That((noParam.StatusCode, wrongGrader.StatusCode, none.StatusCode, noRun.StatusCode),
            Is.EqualTo((HttpStatusCode.BadRequest, HttpStatusCode.BadRequest, HttpStatusCode.NotFound, HttpStatusCode.NotFound)));
    }

    [Test]
    public async Task DeletingTheRun_DeletesItsReviewsToo()
    {
        await using var host = await StartAsync();
        await Send(host.Client, HttpMethod.Put, Path("/puls-cook/1/bobotie/meal-for-people.human"), Body());

        var deleted = await Send(host.Client, HttpMethod.Delete, $"/v1/admin/benchmarks/runs/{RunId}");

        Assert.That(deleted.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(await host.Reviews.OfRunAsync(RunId), Is.Empty);
    }
}
