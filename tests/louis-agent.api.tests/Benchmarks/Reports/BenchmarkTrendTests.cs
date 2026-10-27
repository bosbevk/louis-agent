using LouisAgent.Testing;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Api.Benchmarks.Reports;
using System.Text.Json;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;
using static LouisAgent.Api.Benchmarks.Runs.BenchmarkProtocol;

namespace LouisAgent.Api.Tests.Benchmarks.Reports;

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
    private async Task Run(string id, string check, int hour, int repeat, BenchmarkRunStatus status, decimal? cookCost, int right,
        IReadOnlyList<ModelUse>? models = null)
    {
        await _store.SaveRunAsync(new Run(id, check, repeat, status, T0.AddHours(hour), Models: models));
        int order = 0;
        for (int r = 1; r <= repeat; r++)
            foreach (string mode in new[] { SampleMode.Alpha, SampleMode.Beta })
                foreach (string task in new[] { "cook-af", "control-web" })
                {
                    await _store.SaveConversationAsync(id, new Conversation(new ConversationKey(mode, r, task), ++order, null, Scores(order <= right), 0, 1));
                    decimal? cost = mode == SampleMode.Beta ? cookCost : 0.01m;
                    await _store.AddRequestsAsync(id, [new Request(id, new TurnKey(mode, r, task, 1), T0, "turn", "s", null, "m", 1, 1, 0, 0, cost, 10)]);
                }
    }

    [Test]
    public async Task AHeldOutRun_IsNeverAPointOfTheDevTrend_AndARunBeforeSplitsIs()
    {
        // F18 step 5.4 review: a held-out run is stored like a dev one, and would join the same trend. A run that names no split (one before they were
        // kept) is a dev run, as it always was; a run of all of them has held-out conversations among its own.
        await _store.SaveRunAsync(new Run("dev", "puls-cook", 1, BenchmarkRunStatus.Done, T0, Split: BenchmarkRunSplit.Dev));
        await _store.SaveRunAsync(new Run("old", "puls-cook", 1, BenchmarkRunStatus.Done, T0.AddHours(1)));
        await _store.SaveRunAsync(new Run("held", "puls-cook", 1, BenchmarkRunStatus.Done, T0.AddHours(2), Split: BenchmarkRunSplit.HeldOut));
        await _store.SaveRunAsync(new Run("all", "puls-cook", 1, BenchmarkRunStatus.Done, T0.AddHours(3), Split: BenchmarkRunSplit.All));
        foreach (string id in new[] { "dev", "old", "held", "all" })
            await _store.SaveConversationAsync(id, new Conversation(new ConversationKey(SampleMode.Beta, 1, "cook-af"), 1, null, Scores(true), 0, 1));

        var trend = await BenchmarkTrends.ComputeAsync(_store, "puls-cook");

        Assert.That(trend.Modes.Single().Points.Select(p => p.Run), Is.EqualTo(new[] { "dev", "old" }));
    }

    [Test]
    public async Task AReplayOfAnotherRun_IsNeverAPoint_BecauseItsScoresAreThatRunsAndItCostsNothing()
    {
        // F18 step 5.5: a run started with replay is the recorded run again (its scores equal it, its cost is 0): drawn on the trend it would be a second
        // point of the same run, with a cost of 0.
        await _store.SaveRunAsync(new Run("live", "puls-cook", 1, BenchmarkRunStatus.Done, T0));
        await _store.SaveRunAsync(new Run("again", "puls-cook", 1, BenchmarkRunStatus.Done, T0.AddHours(1),
            Settings: new Dictionary<string, string> { ["replay_of"] = "live" }));
        foreach (string id in new[] { "live", "again" })
            await _store.SaveConversationAsync(id, new Conversation(new ConversationKey(SampleMode.Beta, 1, "cook-af"), 1, null, Scores(true), 0, 1));

        var trend = await BenchmarkTrends.ComputeAsync(_store, "puls-cook");

        Assert.That(trend.Modes.Single().Points.Select(p => p.Run), Is.EqualTo(new[] { "live" }));
    }

    [Test]
    public async Task AReScore_IsNeverAPoint_AndTheOriginalRunsPointIsUnchanged()
    {
        // F18 step 5b: a trend point means "the agent was measured here". A re-score grades transcripts recorded on another day, so it adds no point.
        await _store.SaveRunAsync(new Run("live", "puls-cook", 1, BenchmarkRunStatus.Done, T0));
        await _store.SaveConversationAsync("live", new Conversation(new ConversationKey(SampleMode.Beta, 1, "cook-af"), 1, null, Scores(true), 0, 1));
        var before = (await BenchmarkTrends.ComputeAsync(_store, "puls-cook")).Modes.Single().Points.Single();
        await _store.SaveRunAsync(new Run("graded-again", "puls-cook", 1, BenchmarkRunStatus.Done, T0.AddDays(3), Total: new Total(0m, "USD", 0, 0), RescoreOf: "live"));
        await _store.SaveConversationAsync("graded-again", new Conversation(new ConversationKey(SampleMode.Beta, 1, "cook-af"), 1, null, Scores(false), 0, 1));

        var points = (await BenchmarkTrends.ComputeAsync(_store, "puls-cook")).Modes.Single().Points;

        Assert.That(points.Select(p => p.Run), Is.EqualTo(new[] { "live" }));
        Assert.That(points.Single(), Is.EqualTo(before));
    }

    [Test]
    public async Task EachFinishedRun_IsAPointPerMode_OldestFirst()
    {
        await Run("b", "cook", hour: 2, repeat: 1, BenchmarkRunStatus.Done, cookCost: 0.05m, right: 4);
        await Run("a", "cook", hour: 1, repeat: 1, BenchmarkRunStatus.Done, cookCost: 0.04m, right: 3);
        await Run("live", "cook", hour: 3, repeat: 1, BenchmarkRunStatus.Running, cookCost: 0.09m, right: 4);
        await Run("other", "routing", hour: 1, repeat: 1, BenchmarkRunStatus.Done, cookCost: 1m, right: 4);

        var trend = await BenchmarkTrends.ComputeAsync(_store, "cook");

        Assert.That(trend.Modes.Select(m => m.Name), Is.EqualTo(new[] { SampleMode.Alpha, SampleMode.Beta }));
        var cook = trend.Modes.Single(m => m.Name == SampleMode.Beta);
        Assert.That(cook.Points.Select(p => p.Run), Is.EqualTo(new[] { "a", "b" }), "a run still going isn't a point yet");
        Assert.That(cook.Points.Select(p => p.Cost), Is.EqualTo(new decimal?[] { 0.08m, 0.10m }), "two conversations each");
        Assert.That(cook.Points[0].At, Is.EqualTo(T0.AddHours(1)));
    }

    [Test]
    public async Task ARunOnAnotherModel_IsAnotherLine_AndAModeWithoutOneKeepsItsName()
    {
        await Run("a", "cook", hour: 1, repeat: 1, BenchmarkRunStatus.Done, cookCost: 0.04m, right: 3,
            models: [new ModelUse(SampleMode.Beta, BenchmarkModelLevel.Agent, "puls-cook", "claude-haiku-5-5")]);
        await Run("b", "cook", hour: 2, repeat: 1, BenchmarkRunStatus.Done, cookCost: 0.05m, right: 3,
            models: [new ModelUse(SampleMode.Beta, BenchmarkModelLevel.Agent, "puls-cook", "claude-sonnet-5-5")]);

        var trend = await BenchmarkTrends.ComputeAsync(_store, "cook");

        Assert.That(trend.Modes.Select(m => m.Name), Is.EquivalentTo(new[]
        {
            SampleMode.Alpha, $"{SampleMode.Beta} (claude-haiku-5-5)", $"{SampleMode.Beta} (claude-sonnet-5-5)",
        }));
        Assert.That(trend.Modes.Single(m => m.Name == $"{SampleMode.Beta} (claude-sonnet-5-5)").Points.Select(p => p.Run), Is.EqualTo(new[] { "b" }));
        Assert.That(trend.Modes.Single(m => m.Name == SampleMode.Alpha).Points.Select(p => p.Run), Is.EqualTo(new[] { "a", "b" }));
    }

    [Test]
    public async Task TheScore_IsTheShareRoutedRight_PerMode()
    {
        // Order: puls-only cook-af (1), puls-only control-web (2), puls-cook cook-af (3), puls-cook control-web (4).
        await Run("a", "cook", hour: 1, repeat: 1, BenchmarkRunStatus.Done, cookCost: 0.04m, right: 3);

        var trend = await BenchmarkTrends.ComputeAsync(_store, "cook");

        Assert.That(trend.Modes.Single(m => m.Name == SampleMode.Alpha).Points.Single().Score, Is.EqualTo(1.0));
        Assert.That(trend.Modes.Single(m => m.Name == SampleMode.Beta).Points.Single().Score, Is.EqualTo(0.5));
        Assert.That(trend.Modes.Single(m => m.Name == SampleMode.Beta).Points.Single().Conversations, Is.EqualTo(2));
    }

    [Test]
    public async Task ARunOfSeveralRepeats_IsWhatOneRepeatCost()
    {
        await Run("a", "cook", hour: 1, repeat: 2, BenchmarkRunStatus.Done, cookCost: 0.04m, right: 8);

        var point = (await BenchmarkTrends.ComputeAsync(_store, "cook")).Modes.Single(m => m.Name == SampleMode.Beta).Points.Single();

        Assert.That(point.Cost, Is.EqualTo(0.08m), "four conversations over two repeats");
        Assert.That(point.Conversations, Is.EqualTo(4));
    }

    [Test]
    public async Task ARunWithNoPricedRequest_HasAnUnknownCost_NotZero()
    {
        await Run("a", "cook", hour: 1, repeat: 1, BenchmarkRunStatus.Done, cookCost: null, right: 4);

        var point = (await BenchmarkTrends.ComputeAsync(_store, "cook")).Modes.Single(m => m.Name == SampleMode.Beta).Points.Single();

        Assert.That(point.Cost, Is.Null);
    }

    // FX24 follow-up (Louis, 2026-10-26, the trend's Accuracy panel fell to 0 the day the cook began giving estimates): an estimated row and a row rightly without a price in a town with no
    // shop count as accurate in the trend, as they do in the run explorer's accuracy ring (AccuracyRing); only a guess and a row with no price do not.
    [Test]
    public async Task AnEstimatedRow_AndARowRightlyWithoutAPrice_CountAsAccurate_InTheTrend()
    {
        await _store.SaveRunAsync(new Run("a", "cook", 1, BenchmarkRunStatus.Done, T0));
        JsonElement S(string json) => JsonDocument.Parse(json).RootElement.Clone();
        await _store.SaveConversationAsync("a", new Conversation(new ConversationKey(SampleMode.Beta, 1, "cook-af"), 1, "puls-cook",
            S("""{"routed_right":true,"ingredients":5,"priced":0,"estimated":4,"unsourced":1,"priced_ratio":0.8}"""), 0, 10));
        await _store.SaveConversationAsync("a", new Conversation(new ConversationKey(SampleMode.Beta, 1, "cook-recipe-only"), 2, "puls-cook",
            S("""{"routed_right":true,"ingredients":5,"priced":0,"estimated":0,"no_shop_rows":5,"priced_ratio":1}"""), 0, 10));

        var point = (await BenchmarkTrends.ComputeAsync(_store, "cook")).Modes.Single().Points.Single();

        Assert.That(point.Sourced, Is.EqualTo(0.9), "4 estimated and 5 rightly unpriced of 10; the one guess is not");
    }

    [Test]
    public async Task EveryRingHasItsLine_TheTokensTimeCacheAccuracyToolsValueAndSites()
    {
        // Louis: the trend as the nine rings over the runs. One run, one mode, two meal conversations: one priced 4 of
        // 5 with sources (a real answer), one guessed; each with a turn of 10 s reading two sites, one a shop; the
        // first turn's search refused once of two calls.
        await _store.SaveRunAsync(new Run("a", "cook", 1, BenchmarkRunStatus.Done, T0));
        JsonElement S(string json) => JsonDocument.Parse(json).RootElement.Clone();
        await _store.SaveConversationAsync("a", new Conversation(new ConversationKey(SampleMode.Beta, 1, "cook-af"), 1, "puls-cook",
            S("""{"routed_right":true,"ingredients":5,"priced":4,"unsourced":0,"priced_ratio":0.8}"""), 0, 10));
        await _store.SaveConversationAsync("a", new Conversation(new ConversationKey(SampleMode.Beta, 1, "cook-en"), 2, "puls-cook",
            S("""{"routed_right":true,"ingredients":5,"priced":0,"unsourced":5,"priced_ratio":0}"""), 0, 10));
        StreamEvent E(string type, object data) => new(type, JsonSerializer.SerializeToElement(data), null);
        StreamEvent Use(string id) => E("content_block_start", new { content_block = new { type = "tool_use", id, name = "WebSearch", input = new { } } });
        StreamEvent Refused(string id) => E("content_block_start", new { content_block = new { type = "tool_result", tool_use_id = id, content = "Error: limit", is_error = true } });
        await _store.SaveTurnAsync("a", new Turn(new TurnKey(SampleMode.Beta, 1, "cook-af", 1), "s1", "?", "!", "puls-cook", null, 1, 2,
            ["checkers.co.za", "food.com"], 10, null), [Use("u1"), Refused("u1"), Use("u2")]);
        await _store.SaveTurnAsync("a", new Turn(new TurnKey(SampleMode.Beta, 1, "cook-en", 1), "s2", "?", "!", "puls-cook", null, 1, 0,
            ["checkers.co.za"], 20, null), []);
        Request R(string task, decimal cost, long input, long cached) =>
            new("a", new TurnKey(SampleMode.Beta, 1, task, 1), T0, "turn", "s", null, "m", input, 100, cached, 0, cost, 1000);
        await _store.AddRequestsAsync("a", [R("cook-af", 0.06m, 200, 700), R("cook-en", 0.02m, 100, 0)]);

        var point = (await BenchmarkTrends.ComputeAsync(_store, "cook")).Modes.Single().Points.Single();

        Assert.That(point.Tokens, Is.EqualTo(1200L), "input, output and cache, per repeat");
        Assert.That(point.SecondsPerTurn, Is.EqualTo(15.0));
        Assert.That(point.CacheShare, Is.EqualTo(0.7).Within(1e-9), "700 of the 1,000 input tokens from the cache");
        Assert.That(point.Sourced, Is.EqualTo(0.4), "4 of 10 ingredients priced with a source");
        Assert.That(point.ToolsFailed, Is.EqualTo(0.5), "one of two tool calls refused");
        Assert.That(point.CostPerAnswer, Is.EqualTo(0.06m), "one real answer, cook-af's cost");
        Assert.That(point.Sites, Is.EqualTo(2));
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
