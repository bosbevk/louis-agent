using System.Net;
using System.Text.Json;
using LouisAgent.Api.Evals.Reviews;
using LouisAgent.Api.Evals.Reviews.Store;
using LouisAgent.Api.Evals.Runs.Kinds;
using LouisAgent.Api.Evals.Store;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Protocol.Parts.Conversations;
using LouisAgent.Testing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Evals.Tests.Evals.Reviews.Queue;

/// <summary>
/// The review queue (F53.3 step 1): every answer waiting for a person across the stored runs, as one route. A
/// reviewer's queue leaves out what they graded; nobody's leaves out what anyone graded; a run without a human
/// review adds nothing; each item carries what was asked and answered; a limit cuts the items and keeps the total;
/// the route needs the key.
/// </summary>
public class ReviewQueueTests
{
    private const string AdminKey = "admin-secret";
    private static readonly DateTimeOffset T0 = new(2026, 10, 26, 9, 0, 0, TimeSpan.Zero);

    /// <summary>The running API with its client and the stores.</summary>
    private sealed record Host(WebApplication App, HttpClient Client, InMemoryEvalStore Store,
        InMemoryHumanReviewStore Reviews) : IAsyncDisposable
    {
        /// <summary>Stops the API when the test is done.</summary>
        public async ValueTask DisposeAsync() => await App.DisposeAsync();
    }

    /// <summary>
    /// Three runs: run_2 (newest) and run_1 have the judge and its human review, each with a conversation that
    /// waits; run_1 has a second one Louis graded; run_0 has no human review at all.
    /// </summary>
    private static async Task<Host> StartAsync()
    {
        var store = new InMemoryEvalStore();
        var reviews = new InMemoryHumanReviewStore();
        Grader[] reviewed =
        [
            new("meal-for-people", 1, CheckKind.Model),
            new("meal-for-people.human", 1, CheckKind.Human)
        ];
        await store.SaveRunAsync(new Run("run_2", SampleEval.Beta, 1, EvalRunStatus.Done, T0.AddDays(1),
            T0.AddDays(1).AddMinutes(5), 0, Graders: reviewed, Eval: SampleEval.Beta));
        await store.SaveRunAsync(new Run("run_1", SampleEval.Beta, 1, EvalRunStatus.Done, T0, T0.AddMinutes(5), 0,
            Graders: reviewed, Eval: SampleEval.Beta));
        await store.SaveRunAsync(new Run("run_0", SampleEval.Alpha, 1, EvalRunStatus.Done, T0.AddDays(-1),
            T0.AddDays(-1).AddMinutes(5), 0, Graders: [new Grader("cook-no-error", 1, CheckKind.FixedRule)]));

        await ConversationAsync(store, "run_2", "bobotie", "Bobotie vir vier");
        await ConversationAsync(store, "run_1", "sosaties", "Net 'n sousie");
        await ConversationAsync(store, "run_1", "melktert", "Melktert met kaneel");
        await ConversationAsync(store, "run_0", "bobotie", "Bobotie", reviewed: false);
        await reviews.AddAsync(new HumanGrade("run_1", new ConversationKey(SampleMode.Beta, 1, "melktert"),
            "meal-for-people", HumanVerdict.Pass, "", "Louis", T0.AddHours(1)));

        var app = ApiTestHost.Build(adminKey: AdminKey, store: store, reviews: reviews);
        await app.StartAsync();
        return new Host(app, app.GetTestClient(), store, reviews);
    }

    /// <summary>A conversation with the judge's pass and, when reviewed, the pending human review; two turns.</summary>
    private static async Task ConversationAsync(InMemoryEvalStore store, string run, string task, string answer,
        bool reviewed = true)
    {
        var key = new ConversationKey(SampleMode.Beta, 1, task);
        List<Grade> grades = [new("meal-for-people", 1, 1, GradeOutcome.Pass, "the judge's reason")];
        if (reviewed) grades.Add(new Grade("meal-for-people.human", 1, 0, GradeOutcome.Pending, ""));
        await store.SaveConversationAsync(run, new Conversation(key, 1, null, null, 0, 1.0, grades));
        await store.SaveTurnAsync(run, new Turn(new TurnKey(SampleMode.Beta, 1, task, 1), "s1", "Wat eet ons?",
            "Iets lekkers", SampleRole.Alpha, null, 1, 1, [], 1.0, null), []);
        await store.SaveTurnAsync(run, new Turn(new TurnKey(SampleMode.Beta, 1, task, 2), "s1", $"Maak {task}.",
            answer, SampleRole.Alpha, null, 1, 1, [], 1.0, null), []);
    }

    /// <summary>The queue, with whatever query follows.</summary>
    private static string Path(string query = "") => $"/v1/admin/evals/reviews/queue{query}";

    /// <summary>Sends a GET, with the admin key unless the test leaves it off.</summary>
    private static Task<HttpResponseMessage> Send(HttpClient client, string url, bool withKey = true)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (withKey) request.Headers.Add("x-api-key", AdminKey);
        return client.SendAsync(request);
    }

    /// <summary>Reads an answer's body as JSON.</summary>
    private static async Task<JsonElement> JsonOf(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();

    /// <summary>Each item's run and task, in the queue's order.</summary>
    private static List<(string Run, string Task)> Where(JsonElement reply) =>
        reply.GetProperty("items").EnumerateArray()
            .Select(i => (i.GetProperty("run").GetString()!,
                i.GetProperty("conversation").GetProperty("task").GetString()!))
            .ToList();

    /// <summary>Louis's queue leaves out what Louis graded, newest run first; the total says how many wait.</summary>
    [Test]
    public async Task AReviewersQueue_LeavesOutWhatTheyGraded()
    {
        await using var host = await StartAsync();

        var reply = await JsonOf(await Send(host.Client, Path("?reviewer=louis")));

        Assert.That(Where(reply), Is.EqualTo(new[] { ("run_2", "bobotie"), ("run_1", "sosaties") }));
        Assert.That(reply.GetProperty("total").GetInt32(), Is.EqualTo(2));
    }

    /// <summary>With no name, the queue is what nobody graded: the same two here, plus nothing from run_0.</summary>
    [Test]
    public async Task NobodysQueue_IsWhatNobodyGraded_AndARunWithoutAReviewAddsNothing()
    {
        await using var host = await StartAsync();
        await host.Reviews.AddAsync(new HumanGrade("run_1", new ConversationKey(SampleMode.Beta, 1, "sosaties"),
            "meal-for-people", HumanVerdict.Fail, "", "Anna", T0.AddHours(2)));

        var nobody = await JsonOf(await Send(host.Client, Path()));
        var louis = await JsonOf(await Send(host.Client, Path("?reviewer=Louis")));

        Assert.That(Where(nobody), Is.EqualTo(new[] { ("run_2", "bobotie") }), "Anna graded sosaties");
        Assert.That(Where(louis), Is.EqualTo(new[] { ("run_2", "bobotie"), ("run_1", "sosaties") }),
            "Louis hasn't graded sosaties");
    }

    /// <summary>
    /// An item carries where it is from, the run's graders, the conversation's row with its grades and the
    /// people's, the questions asked and the last answer.
    /// </summary>
    [Test]
    public async Task AnItem_CarriesTheRunTheGradersTheConversationAndTheTurns()
    {
        await using var host = await StartAsync();

        var item = (await JsonOf(await Send(host.Client, Path("?reviewer=Louis")))).GetProperty("items")[0];

        Assert.That((item.GetProperty("run").GetString(), item.GetProperty("eval").GetString()),
            Is.EqualTo(("run_2", SampleEval.Beta)));
        Assert.That(item.GetProperty("graders").EnumerateArray().Select(g => g.GetProperty("name").GetString()),
            Is.EqualTo(new[] { "meal-for-people", "meal-for-people.human" }));
        var conversation = item.GetProperty("conversation");
        Assert.That(conversation.GetProperty("mode").GetString(), Is.EqualTo(SampleMode.Beta));
        Assert.That(
            conversation.GetProperty("grades").EnumerateArray().Select(g => g.GetProperty("outcome").GetString()),
            Is.EqualTo(new[] { "pass", "pending" }));
        Assert.That(item.GetProperty("asked").EnumerateArray().Select(a => a.GetString()),
            Is.EqualTo(new[] { "Wat eet ons?", "Maak bobotie." }));
        Assert.That(item.GetProperty("answer").GetString(), Is.EqualTo("Bobotie vir vier"));
    }

    /// <summary>A limit cuts the items but the total still counts every answer that waits.</summary>
    [Test]
    public async Task ALimit_CutsTheItems_AndKeepsTheTotal()
    {
        await using var host = await StartAsync();

        var reply = await JsonOf(await Send(host.Client, Path("?reviewer=Louis&limit=1")));

        Assert.That(Where(reply), Is.EqualTo(new[] { ("run_2", "bobotie") }));
        Assert.That(reply.GetProperty("total").GetInt32(), Is.EqualTo(2));
    }

    /// <summary>The queue is behind the admin key.</summary>
    [Test]
    public async Task WithoutTheKey_TheQueueIsRefused()
    {
        await using var host = await StartAsync();

        var response = await Send(host.Client, Path(), withKey: false);

        Assert.That(response.StatusCode, Is.AnyOf(HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized));
    }
}
