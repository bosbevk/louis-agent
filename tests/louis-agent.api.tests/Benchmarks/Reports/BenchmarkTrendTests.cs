using LouisAgent.Testing;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Api.Benchmarks.Reports;
using System.Text.Json;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;
using static LouisAgent.Api.Benchmarks.Runs.BenchmarkProtocol;

namespace LouisAgent.Api.Tests.Benchmarks.Reports;

/// <summary>
/// An eval's trend over its runs, per mode: what one repeat cost and how many conversations went where they should.
/// It's worked out in C# over the store, so it comes out the same on the in-memory store as on MongoDB.
/// </summary>
public class BenchmarkTrendTests
{
    /// <summary>When the first run started; each run starts some hours after.</summary>
    private static readonly DateTimeOffset T0 = new(2026, 10, 17, 9, 0, 0, TimeSpan.Zero);

    /// <summary>A fresh store for each test.</summary>
    private InMemoryBenchmarkStore _store = null!;

    /// <summary>
    /// NUnit keeps one instance for the whole fixture, so each test gets its own store and never sees another's runs.
    /// </summary>
    [SetUp]
    public void SetUp() => _store = new InMemoryBenchmarkStore();

    /// <summary>A conversation's scores, routed right or not.</summary>
    private static JsonElement Scores(bool right) =>
        JsonDocument.Parse($$"""{"routed_right":{{(right ? "true" : "false")}}}""").RootElement.Clone();

    /// <summary>
    /// Saves a run of the eval: two modes, two tasks each, per repeat. The cook mode's conversations cost
    /// <paramref name="cookCost"/> each (the other a cent), and the first <paramref name="right"/> are routed right.
    /// </summary>
    private async Task Run(string id, string check, int hour, int repeat, BenchmarkRunStatus status, decimal? cookCost,
        int right,
        IReadOnlyList<ModelUse>? models = null)
    {
        await _store.SaveRunAsync(new Run(id, check, repeat, status, T0.AddHours(hour), Models: models));

        // Each conversation numbered in order, with one priced request on its first turn.
        int order = 0;
        for (int r = 1; r <= repeat; r++)
            foreach (string mode in new[] { SampleMode.Alpha, SampleMode.Beta })
                foreach (string task in new[] { SampleScenario.Alpha, "control-web" })
                {
                    await _store.SaveConversationAsync(id,
                        new Conversation(new ConversationKey(mode, r, task), ++order, null, Scores(order <= right), 0, 1));
                    decimal? cost = mode == SampleMode.Beta ? cookCost : 0.01m;
                    await _store.AddRequestsAsync(id,
                        [new Request(id, new TurnKey(mode, r, task, 1), T0, "turn", "s", null, "m", 1, 1, 0, 0, cost, 10)]);
                }
    }

    /// <summary>
    /// A held-out run never joins the dev trend. A run from before splits were kept counts as dev, as it always did,
    /// and a run of every split is left out because it holds held-out conversations.
    /// </summary>
    [Test]
    public async Task AHeldOutRun_IsNeverAPointOfTheDevTrend_AndARunBeforeSplitsIs()
    {
        // A dev run, one with no split, a held-out run and a run of all splits, each with one conversation.
        await _store.SaveRunAsync(new Run("dev", SampleEval.Beta, 1, BenchmarkRunStatus.Done, T0,
            Split: BenchmarkRunSplit.Dev));
        await _store.SaveRunAsync(new Run("old", SampleEval.Beta, 1, BenchmarkRunStatus.Done, T0.AddHours(1)));
        await _store.SaveRunAsync(new Run("held", SampleEval.Beta, 1, BenchmarkRunStatus.Done, T0.AddHours(2),
            Split: BenchmarkRunSplit.HeldOut));
        await _store.SaveRunAsync(new Run("all", SampleEval.Beta, 1, BenchmarkRunStatus.Done, T0.AddHours(3),
            Split: BenchmarkRunSplit.All));
        foreach (string id in new[] { "dev", "old", "held", "all" })
            await _store.SaveConversationAsync(id,
                new Conversation(new ConversationKey(SampleMode.Beta, 1, SampleScenario.Alpha), 1, null, Scores(true),
                    0, 1));

        var trend = await BenchmarkTrends.ComputeAsync(_store, SampleEval.Beta);

        Assert.That(trend.Modes.Single().Points.Select(p => p.Run), Is.EqualTo(new[] { "dev", "old" }));
    }

    /// <summary>
    /// A replay is the recorded run again, with the same scores and no cost, so it's never a second point for that
    /// run.
    /// </summary>
    [Test]
    public async Task AReplayOfAnotherRun_IsNeverAPoint_BecauseItsScoresAreThatRunsAndItCostsNothing()
    {
        await _store.SaveRunAsync(new Run("live", SampleEval.Beta, 1, BenchmarkRunStatus.Done, T0));
        await _store.SaveRunAsync(new Run("again", SampleEval.Beta, 1, BenchmarkRunStatus.Done, T0.AddHours(1),
            Settings: new Dictionary<string, string> { ["replay_of"] = "live" }));
        foreach (string id in new[] { "live", "again" })
            await _store.SaveConversationAsync(id,
                new Conversation(new ConversationKey(SampleMode.Beta, 1, SampleScenario.Alpha), 1, null, Scores(true),
                    0, 1));

        var trend = await BenchmarkTrends.ComputeAsync(_store, SampleEval.Beta);

        Assert.That(trend.Modes.Single().Points.Select(p => p.Run), Is.EqualTo(new[] { "live" }));
    }

    /// <summary>
    /// A point means the agent was measured there. A re-score grades transcripts from another day, so it adds no point
    /// and leaves the original run's point as it was.
    /// </summary>
    [Test]
    public async Task AReScore_IsNeverAPoint_AndTheOriginalRunsPointIsUnchanged()
    {
        // The live run and its point before any re-score.
        await _store.SaveRunAsync(new Run("live", SampleEval.Beta, 1, BenchmarkRunStatus.Done, T0));
        await _store.SaveConversationAsync("live",
            new Conversation(new ConversationKey(SampleMode.Beta, 1, SampleScenario.Alpha), 1, null, Scores(true), 0,
                1));
        var before = (await BenchmarkTrends.ComputeAsync(_store, SampleEval.Beta)).Modes.Single().Points.Single();

        // Three days later the same transcripts are graded again, with another score.
        await _store.SaveRunAsync(new Run("graded-again", SampleEval.Beta, 1, BenchmarkRunStatus.Done, T0.AddDays(3),
            Total: new Total(0m, "USD", 0, 0), RescoreOf: "live"));
        await _store.SaveConversationAsync("graded-again",
            new Conversation(new ConversationKey(SampleMode.Beta, 1, SampleScenario.Alpha), 1, null, Scores(false), 0,
                1));

        var points = (await BenchmarkTrends.ComputeAsync(_store, SampleEval.Beta)).Modes.Single().Points;

        Assert.That(points.Select(p => p.Run), Is.EqualTo(new[] { "live" }));
        Assert.That(points.Single(), Is.EqualTo(before));
    }

    /// <summary>
    /// Each finished run of the eval is a point for every mode, oldest first; a run still going and another eval's
    /// run are not.
    /// </summary>
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
        Assert.That(cook.Points.Select(p => p.Run), Is.EqualTo(new[] { "a", "b" }),
            "a run still going isn't a point yet");
        Assert.That(cook.Points.Select(p => p.Cost), Is.EqualTo(new decimal?[] { 0.08m, 0.10m }),
            "two conversations each");
        Assert.That(cook.Points[0].At, Is.EqualTo(T0.AddHours(1)));
    }

    /// <summary>
    /// A mode run on another model is another line, named with its model; a mode the run names no model for keeps
    /// its plain name.
    /// </summary>
    [Test]
    public async Task ARunOnAnotherModel_IsAnotherLine_AndAModeWithoutOneKeepsItsName()
    {
        await Run("a", "cook", hour: 1, repeat: 1, BenchmarkRunStatus.Done, cookCost: 0.04m, right: 3,
            models: [new ModelUse(SampleMode.Beta, BenchmarkModelLevel.Agent, SampleRole.Alpha, "claude-haiku-5-5")]);
        await Run("b", "cook", hour: 2, repeat: 1, BenchmarkRunStatus.Done, cookCost: 0.05m, right: 3,
            models: [new ModelUse(SampleMode.Beta, BenchmarkModelLevel.Agent, SampleRole.Alpha, "claude-sonnet-5-5")]);

        var trend = await BenchmarkTrends.ComputeAsync(_store, "cook");

        Assert.That(trend.Modes.Select(m => m.Name),
            Is.EquivalentTo(new[]
            {
                SampleMode.Alpha, $"{SampleMode.Beta} (claude-haiku-5-5)", $"{SampleMode.Beta} (claude-sonnet-5-5)",
            }));
        Assert.That(
            trend.Modes.Single(m => m.Name == $"{SampleMode.Beta} (claude-sonnet-5-5)").Points.Select(p => p.Run),
            Is.EqualTo(new[] { "b" }));
        Assert.That(trend.Modes.Single(m => m.Name == SampleMode.Alpha).Points.Select(p => p.Run),
            Is.EqualTo(new[] { "a", "b" }));
    }

    /// <summary>A point's score is the share of the mode's conversations routed right.</summary>
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

    /// <summary>A run of several repeats shows what one repeat cost.</summary>
    [Test]
    public async Task ARunOfSeveralRepeats_IsWhatOneRepeatCost()
    {
        await Run("a", "cook", hour: 1, repeat: 2, BenchmarkRunStatus.Done, cookCost: 0.04m, right: 8);

        var point = (await BenchmarkTrends.ComputeAsync(_store, "cook")).Modes.Single(m => m.Name == SampleMode.Beta)
            .Points.Single();

        Assert.That(point.Cost, Is.EqualTo(0.08m), "four conversations over two repeats");
        Assert.That(point.Conversations, Is.EqualTo(4));
    }

    /// <summary>A run where nothing is priced has an unknown cost, not zero.</summary>
    [Test]
    public async Task ARunWithNoPricedRequest_HasAnUnknownCost_NotZero()
    {
        await Run("a", "cook", hour: 1, repeat: 1, BenchmarkRunStatus.Done, cookCost: null, right: 4);

        var point = (await BenchmarkTrends.ComputeAsync(_store, "cook")).Modes.Single(m => m.Name == SampleMode.Beta)
            .Points.Single();

        Assert.That(point.Cost, Is.Null);
    }

    /// <summary>
    /// An estimated row, and a row rightly without a price in a town with no shop, count as accurate in the trend,
    /// as they do in the run explorer's accuracy ring; only a guess does not. The trend's accuracy fell to 0 the day
    /// the cook began giving estimates.
    /// </summary>
    [Test]
    public async Task AnEstimatedRow_AndARowRightlyWithoutAPrice_CountAsAccurate_InTheTrend()
    {
        // One meal with four estimates and one guess, one in a town with no shop.
        await _store.SaveRunAsync(new Run("a", "cook", 1, BenchmarkRunStatus.Done, T0));
        JsonElement S(string json) => JsonDocument.Parse(json).RootElement.Clone();
        await _store.SaveConversationAsync("a", new Conversation(
            new ConversationKey(SampleMode.Beta, 1, SampleScenario.Alpha), 1, SampleRole.Alpha,
            S("""{"routed_right":true,"ingredients":5,"priced":0,"estimated":4,"unsourced":1,"priced_ratio":0.8}"""), 0,
            10));
        await _store.SaveConversationAsync("a", new Conversation(
            new ConversationKey(SampleMode.Beta, 1, "cook-recipe-only"), 2, SampleRole.Alpha,
            S("""{"routed_right":true,"ingredients":5,"priced":0,"estimated":0,"no_shop_rows":5,"priced_ratio":1}"""),
            0, 10));

        var point = (await BenchmarkTrends.ComputeAsync(_store, "cook")).Modes.Single().Points.Single();

        Assert.That(point.Sourced, Is.EqualTo(0.9), "4 estimated and 5 rightly unpriced of 10; the one guess is not");
    }

    /// <summary>
    /// The trend has a line for every ring of the run explorer: tokens, time, cache, accuracy, tools, value and
    /// sites.
    /// </summary>
    [Test]
    public async Task EveryRingHasItsLine_TheTokensTimeCacheAccuracyToolsValueAndSites()
    {
        // One run, one mode, two meal conversations: one priced 4 of 5 with sources (a real answer), one guessed.
        await _store.SaveRunAsync(new Run("a", "cook", 1, BenchmarkRunStatus.Done, T0));
        JsonElement S(string json) => JsonDocument.Parse(json).RootElement.Clone();
        await _store.SaveConversationAsync("a", new Conversation(
            new ConversationKey(SampleMode.Beta, 1, SampleScenario.Alpha), 1, SampleRole.Alpha,
            S("""{"routed_right":true,"ingredients":5,"priced":4,"unsourced":0,"priced_ratio":0.8}"""), 0, 10));
        await _store.SaveConversationAsync("a", new Conversation(
            new ConversationKey(SampleMode.Beta, 1, SampleScenario.Beta), 2, SampleRole.Alpha,
            S("""{"routed_right":true,"ingredients":5,"priced":0,"unsourced":5,"priced_ratio":0}"""), 0, 10));

        // A stored stream event, a web search starting, and a search refused by its limit.
        StreamEvent E(string type, object data) => new(type, JsonSerializer.SerializeToElement(data), null);

        StreamEvent Use(string id) => E("content_block_start",
            new
            {
                content_block = new
                {
                    type = "tool_use",
                    id,
                    name = "WebSearch",
                    input = new { }
                }
            });

        StreamEvent Refused(string id) => E("content_block_start",
            new
            {
                content_block = new
                {
                    type = "tool_result",
                    tool_use_id = id,
                    content = "Error: limit",
                    is_error = true
                }
            });

        // Each conversation has one turn reading two sites (one a shop); the first turn's search is refused once of
        // two calls.
        await _store.SaveTurnAsync("a", new Turn(new TurnKey(SampleMode.Beta, 1, SampleScenario.Alpha, 1), "s1", "?",
            "!", SampleRole.Alpha, null, 1, 2,
            ["checkers.co.za", "food.com"], 10, null), [Use("u1"), Refused("u1"), Use("u2")]);
        await _store.SaveTurnAsync("a", new Turn(new TurnKey(SampleMode.Beta, 1, SampleScenario.Beta, 1), "s2", "?",
            "!", SampleRole.Alpha, null, 1, 0,
            ["checkers.co.za"], 20, null), []);

        // One request per conversation, with its cost and its cached input.
        Request R(string task, decimal cost, long input, long cached) =>
            new("a", new TurnKey(SampleMode.Beta, 1, task, 1), T0, "turn", "s", null, "m", input, 100, cached, 0, cost,
                1000);

        await _store.AddRequestsAsync("a",
            [R(SampleScenario.Alpha, 0.06m, 200, 700), R(SampleScenario.Beta, 0.02m, 100, 0)]);

        var point = (await BenchmarkTrends.ComputeAsync(_store, "cook")).Modes.Single().Points.Single();

        Assert.That(point.Tokens, Is.EqualTo(1200L), "input, output and cache, per repeat");
        Assert.That(point.SecondsPerTurn, Is.EqualTo(15.0));
        Assert.That(point.CacheShare, Is.EqualTo(0.7).Within(1e-9), "700 of the 1,000 input tokens from the cache");
        Assert.That(point.Sourced, Is.EqualTo(0.4), "4 of 10 ingredients priced with a source");
        Assert.That(point.ToolsFailed, Is.EqualTo(0.5), "one of two tool calls refused");
        Assert.That(point.CostPerAnswer, Is.EqualTo(0.06m),
            SampleScenario.Fill("one real answer, <ScenarioAlpha>'s cost"));
        Assert.That(point.Sites, Is.EqualTo(2));
    }

    /// <summary>With a limit, the trend shows only that many of the latest runs.</summary>
    [Test]
    public async Task OnlyTheLastRuns_UpToTheLimit()
    {
        for (int h = 1; h <= 5; h++)
            await Run($"r{h}", "cook", hour: h, repeat: 1, BenchmarkRunStatus.Done, cookCost: 0.04m, right: 4);

        var trend = await BenchmarkTrends.ComputeAsync(_store, "cook", limit: 3);

        Assert.That(trend.Modes[0].Points.Select(p => p.Run), Is.EqualTo(new[] { "r3", "r4", "r5" }));
    }

    /// <summary>An eval with no runs has no lines.</summary>
    [Test]
    public async Task ACheckWithoutRuns_HasNoModes() =>
        Assert.That((await BenchmarkTrends.ComputeAsync(_store, "cook")).Modes, Is.Empty);
}
