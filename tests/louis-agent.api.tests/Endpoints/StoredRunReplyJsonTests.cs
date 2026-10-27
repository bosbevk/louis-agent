using System.Net;
using System.Text.Json;
using LouisAgent.Api.Benchmarks.Reviews;
using LouisAgent.Api.Benchmarks.Reviews.Store;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Testing;
using Microsoft.AspNetCore.TestHost;
using static LouisAgent.Api.Benchmarks.Runs.BenchmarkProtocol;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;

namespace LouisAgent.Api.Tests.Endpoints;

/// <summary>
/// Pins the exact JSON of a stored run's read, the explorer's whole page: a run with every part a run can carry, read
/// over HTTP and compared byte for byte with a golden file (LOUIS_UPDATE_GOLDEN=1 writes it), so the explorer reads it
/// however the endpoint that builds it is shaped.
/// </summary>
public class StoredRunReplyJsonTests
{
    private const string AdminKey = "admin-secret";
    private const string Task = "bobotie";
    private static readonly DateTimeOffset At = new(2026, 10, 25, 9, 0, 0, TimeSpan.Zero);
    private static readonly ConversationKey Key = new(SampleMode.Beta, 1, Task);
    private static readonly TurnKey TurnOne = new(SampleMode.Beta, 1, Task, 1);

    private static string Golden =>
        Path.Combine(ApiTestHost.RepoRoot, "tests", "louis-agent.api.tests", "Endpoints", "StoredRunReply.json");

    /// <summary>
    /// The run's read, with its modes, notes, summaries, conversations, agreement, turns and requests.
    /// </summary>
    [Test]
    public async Task AStoredRunsRead_IsWrittenAsBefore()
    {
        string body = await GetAsync(await StoredAsync(), "/v1/admin/benchmarks/runs/run_1");
        string pretty =
            JsonSerializer.Serialize(JsonDocument.Parse(body), new JsonSerializerOptions { WriteIndented = true });

        if (Environment.GetEnvironmentVariable("LOUIS_UPDATE_GOLDEN") == "1")
            await File.WriteAllTextAsync(Golden, pretty);

        Assert.That(pretty, Is.EqualTo(await File.ReadAllTextAsync(Golden)));
    }

    /// <summary>A turn's events come back with their type, their data as it was sent, and when they came.</summary>
    [Test]
    public async Task ATurnsEvents_AreWrittenAsBefore()
    {
        string body = await GetAsync(await StoredAsync(),
            $"/v1/admin/benchmarks/runs/run_1/turns/{SampleMode.Beta}/1/{Task}/1/events");

        Assert.That(body, Is.EqualTo(
            """{"events":[{"type":"message_start","data":{"role":"assistant"},"at":"2026-10-25T09:00:00+00:00"}]}"""));
    }

    /// <summary>A store and a review store holding one run with every part a run can carry.</summary>
    private static async Task<(InMemoryBenchmarkStore Store, InMemoryHumanReviewStore Reviews)> StoredAsync()
    {
        var store = new InMemoryBenchmarkStore();
        var slice = new BenchmarkSlice(SampleMode.Beta, "overall", 8, 6, 2, 0, 0, ["a"], false, 0.75, 0.75, 0.75, [],
            [], []);
        await store.SaveRunAsync(new Run("run_1", SampleEval.Beta, 1, BenchmarkRunStatus.Done, At, At.AddMinutes(5),
            0, new Dictionary<string, string> { ["WEB"] = "record" },
            [new Mode(SampleMode.Beta, 1, ["web"], SampleRole.Alpha, "haiku", new ModeRates(0.5, 0.25, 2, 0, 1))],
            new Total(1.25m, "USD", 1200, 4), ["started"], Eval: SampleEval.Beta, EvalVersion: 3, Commit: "abc1234",
            Graders:
            [
                new Grader("meal", 1, BenchmarkGraderKind.Model),
                new Grader("meal.human", 1, BenchmarkGraderKind.Human, "Is this a meal?")
            ],
            Slices: [slice]));

        var scores = JsonDocument.Parse("""{"routed_right":true}""").RootElement.Clone();
        await store.SaveConversationAsync("run_1", new Conversation(Key, 1, SampleRole.Alpha, scores, 0, 12.5,
            [new Grade("meal", 1, 1.0, BenchmarkOutcome.Pass, "Good.", "haiku", "abc")], BenchmarkOutcome.Pass));
        var data = JsonDocument.Parse("""{"role":"assistant"}""").RootElement.Clone();
        await store.SaveTurnAsync("run_1",
            new Turn(TurnOne, "sess_1", "Bobotie?", "Here.", SampleRole.Alpha, null, 1, 2, ["www.checkers.co.za"],
                10.5, null),
            [new StreamEvent("message_start", data, At)]);
        await store.AddRequestsAsync("run_1",
            [new Request("run_1", TurnOne, At, "turn", "sess_1", null, "haiku", 10, 5, 0, 0, 0.01m, 100)]);
        await store.AddNoteAsync("run_1", "Slow.", At, null);

        var reviews = new InMemoryHumanReviewStore();
        await reviews.AddAsync(new HumanGrade("run_1", Key, "meal", HumanVerdict.Pass, "Fine.", "louis", At));
        return (store, reviews);
    }

    /// <summary>Asks the admin route over the stores and hands back the body of its 200.</summary>
    private static async Task<string> GetAsync((InMemoryBenchmarkStore Store, InMemoryHumanReviewStore Reviews) stored,
        string path)
    {
        await using var app = ApiTestHost.Build(adminKey: AdminKey, store: stored.Store, reviews: stored.Reviews);
        await app.StartAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("x-api-key", AdminKey);

        using var response = await app.GetTestClient().SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), path);
        return await response.Content.ReadAsStringAsync();
    }
}
