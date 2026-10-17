using System.Text.Json;
using LouisAgent.Api.Services;
using LouisAgent.Api.Services.Benchmarks;
using static LouisAgent.Api.Services.Benchmarks.BenchmarkDocuments;
using static LouisAgent.Api.Services.Benchmarks.BenchmarkProtocol;

namespace LouisAgent.Api.Tests;

// F31 step 11a: a check's trend over its runs, per mode: what one repeat cost and how many conversations went where
// they should. Computed in C# over the store, so it's the same on the in-memory store as on MongoDB.
public class BenchmarkTrendTests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 17, 9, 0, 0, TimeSpan.Zero);
    private InMemoryBenchmarkStore _store = null!;

    // NUnit keeps one instance for the fixture: a store per test, so one test's runs aren't another's.
    [SetUp]
    public void SetUp() => _store = new InMemoryBenchmarkStore();

    private static JsonElement Scores(bool right) => JsonDocument.Parse($$"""{"routed_right":{{(right ? "true" : "false")}}}""").RootElement.Clone();

    /// <summary>A run of <paramref name="check"/> whose modes each cost <paramref name="costs"/> per conversation, and the first <paramref name="right"/> conversations routed right.</summary>
    private async Task Run(string id, string check, int hour, int repeat, BenchmarkRunStatus status, decimal? cookCost, int right)
    {
        await _store.SaveRunAsync(new Run(id, check, repeat, status, T0.AddHours(hour)));
        int order = 0;
        for (int r = 1; r <= repeat; r++)
            foreach (string mode in new[] { "puls-only", "puls-cook" })
                foreach (string task in new[] { "cook-af", "control-web" })
                {
                    await _store.SaveConversationAsync(id, new Conversation(new ConversationKey(mode, r, task), ++order, null, Scores(order <= right), 0, 1));
                    decimal? cost = mode == "puls-cook" ? cookCost : 0.01m;
                    await _store.AddRequestsAsync(id, [new Request(id, new TurnKey(mode, r, task, 1), T0, "turn", "s", null, "m", 1, 1, 0, 0, cost, 10)]);
                }
    }

    [Test]
    public async Task EachFinishedRun_IsAPointPerMode_OldestFirst()
    {
        await Run("b", "cook", hour: 2, repeat: 1, BenchmarkRunStatus.Done, cookCost: 0.05m, right: 4);
        await Run("a", "cook", hour: 1, repeat: 1, BenchmarkRunStatus.Done, cookCost: 0.04m, right: 3);
        await Run("live", "cook", hour: 3, repeat: 1, BenchmarkRunStatus.Running, cookCost: 0.09m, right: 4);
        await Run("other", "routing", hour: 1, repeat: 1, BenchmarkRunStatus.Done, cookCost: 1m, right: 4);

        var trend = await BenchmarkTrends.ComputeAsync(_store, "cook");

        Assert.That(trend.Modes.Select(m => m.Name), Is.EqualTo(new[] { "puls-only", "puls-cook" }));
        var cook = trend.Modes.Single(m => m.Name == "puls-cook");
        Assert.That(cook.Points.Select(p => p.Run), Is.EqualTo(new[] { "a", "b" }), "a run still going isn't a point yet");
        Assert.That(cook.Points.Select(p => p.Cost), Is.EqualTo(new decimal?[] { 0.08m, 0.10m }), "two conversations each");
        Assert.That(cook.Points[0].At, Is.EqualTo(T0.AddHours(1)));
    }

    [Test]
    public async Task TheScore_IsTheShareRoutedRight_PerMode()
    {
        // Order: puls-only cook-af (1), puls-only control-web (2), puls-cook cook-af (3), puls-cook control-web (4).
        await Run("a", "cook", hour: 1, repeat: 1, BenchmarkRunStatus.Done, cookCost: 0.04m, right: 3);

        var trend = await BenchmarkTrends.ComputeAsync(_store, "cook");

        Assert.That(trend.Modes.Single(m => m.Name == "puls-only").Points.Single().Score, Is.EqualTo(1.0));
        Assert.That(trend.Modes.Single(m => m.Name == "puls-cook").Points.Single().Score, Is.EqualTo(0.5));
        Assert.That(trend.Modes.Single(m => m.Name == "puls-cook").Points.Single().Conversations, Is.EqualTo(2));
    }

    [Test]
    public async Task ARunOfSeveralRepeats_IsWhatOneRepeatCost()
    {
        await Run("a", "cook", hour: 1, repeat: 2, BenchmarkRunStatus.Done, cookCost: 0.04m, right: 8);

        var point = (await BenchmarkTrends.ComputeAsync(_store, "cook")).Modes.Single(m => m.Name == "puls-cook").Points.Single();

        Assert.That(point.Cost, Is.EqualTo(0.08m), "four conversations over two repeats");
        Assert.That(point.Conversations, Is.EqualTo(4));
    }

    [Test]
    public async Task ARunWithNoPricedRequest_HasAnUnknownCost_NotZero()
    {
        await Run("a", "cook", hour: 1, repeat: 1, BenchmarkRunStatus.Done, cookCost: null, right: 4);

        var point = (await BenchmarkTrends.ComputeAsync(_store, "cook")).Modes.Single(m => m.Name == "puls-cook").Points.Single();

        Assert.That(point.Cost, Is.Null);
    }

    [Test]
    public async Task OnlyTheLastRuns_UpToTheLimit()
    {
        for (int h = 1; h <= 5; h++) await Run($"r{h}", "cook", hour: h, repeat: 1, BenchmarkRunStatus.Done, cookCost: 0.04m, right: 4);

        var trend = await BenchmarkTrends.ComputeAsync(_store, "cook", limit: 3);

        Assert.That(trend.Modes[0].Points.Select(p => p.Run), Is.EqualTo(new[] { "r3", "r4", "r5" }));
    }

    [Test]
    public async Task ACheckWithoutRuns_HasNoModes() =>
        Assert.That((await BenchmarkTrends.ComputeAsync(_store, "cook")).Modes, Is.Empty);
}
