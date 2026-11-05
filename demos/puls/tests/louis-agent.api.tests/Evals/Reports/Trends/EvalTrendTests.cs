using System.Text.Json;
using LouisAgent.Api.Evals.Reports.Trends;
using LouisAgent.Api.Evals.Runs.Kinds;
using LouisAgent.Api.Evals.Store;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Protocol.Parts.Conversations;
using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Tests.Evals.Reports.Trends;

/// <summary>
/// An eval's trend over its runs, per mode: what one repeat cost and how many conversations passed. It's worked out in
/// C# over the store, so it comes out the same on the in-memory store as on MongoDB.
/// </summary>
public class EvalTrendTests
{
    /// <summary>When the first run started; each run starts some hours after.</summary>
    private static readonly DateTimeOffset T0 = new(2026, 10, 17, 9, 0, 0, TimeSpan.Zero);

    /// <summary>A fresh store for each test.</summary>
    private InMemoryEvalStore _store = null!;

    /// <summary>
    /// NUnit keeps one instance for the whole fixture, so each test gets its own store and never sees another's runs.
    /// </summary>
    [SetUp]
    public void SetUp() => _store = new InMemoryEvalStore();

    /// <summary>A conversation of one task, numbered in its run, with how it came out.</summary>
    private static Conversation Graded(string mode, int repeat, string task, int order, TrialOutcome outcome) =>
        new(new ConversationKey(mode, repeat, task), order, SampleRole.Alpha, null, 0, 10, Outcome: outcome);

    /// <summary>
    /// Saves a run of the eval: two modes, two tasks each, per repeat. The cook mode's conversations cost
    /// <paramref name="cookCost"/> each (the other a cent), and the first <paramref name="passed"/> pass.
    /// </summary>
    private async Task Run(string id, string check, int hour, int repeat, EvalRunStatus status, decimal? cookCost,
        int passed, IReadOnlyList<ModelUse>? models = null)
    {
        await _store.SaveRunAsync(new Run(id, check, repeat, status, T0.AddHours(hour), Models: models));

        // Each conversation numbered in order, with one priced request on its first turn.
        int order = 0;
        for (int r = 1; r <= repeat; r++)
            foreach (string mode in new[] { SampleMode.Alpha, SampleMode.Beta })
                foreach (string task in new[] { SampleScenario.Alpha, "control-web" })
                {
                    order++;
                    var outcome = order <= passed ? TrialOutcome.Pass : TrialOutcome.Fail;
                    await _store.SaveConversationAsync(id, Graded(mode, r, task, order, outcome));
                    decimal? cost = mode == SampleMode.Beta ? cookCost : 0.01m;
                    var key = new TurnKey(mode, r, task, 1);
                    await _store.AddRequestsAsync(id,
                        [new Request(id, key, T0, "turn", "s", null, "m", 1, 1, 0, 0, cost, 10)]);
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
        await _store.SaveRunAsync(new Run("dev", SampleEval.Beta, 1, EvalRunStatus.Done, T0, Split: RunSplit.Dev));
        await _store.SaveRunAsync(new Run("old", SampleEval.Beta, 1, EvalRunStatus.Done, T0.AddHours(1)));
        await _store.SaveRunAsync(new Run("held", SampleEval.Beta, 1, EvalRunStatus.Done, T0.AddHours(2),
            Split: RunSplit.HeldOut));
        await _store.SaveRunAsync(new Run("all", SampleEval.Beta, 1, EvalRunStatus.Done, T0.AddHours(3),
            Split: RunSplit.All));
        foreach (string id in new[] { "dev", "old", "held", "all" })
            await _store.SaveConversationAsync(id,
                Graded(SampleMode.Beta, 1, SampleScenario.Alpha, 1, TrialOutcome.Pass));

        var trend = await EvalTrends.ComputeAsync(_store, SampleEval.Beta);

        Assert.That(trend.Modes.Single().Points.Select(p => p.Run), Is.EqualTo(new[] { "dev", "old" }));
    }

    /// <summary>
    /// A point means the agent was measured there. A re-score grades transcripts from another day, so it adds no point
    /// and leaves the original run's point as it was.
    /// </summary>
    [Test]
    public async Task AReScore_IsNeverAPoint_AndTheOriginalRunsPointIsUnchanged()
    {
        // The live run and its point before any re-score.
        await _store.SaveRunAsync(new Run("live", SampleEval.Beta, 1, EvalRunStatus.Done, T0));
        await _store.SaveConversationAsync("live",
            Graded(SampleMode.Beta, 1, SampleScenario.Alpha, 1, TrialOutcome.Pass));
        var before = (await EvalTrends.ComputeAsync(_store, SampleEval.Beta)).Modes.Single().Points.Single();

        // Three days later the same transcripts are graded again, with another outcome.
        await _store.SaveRunAsync(new Run("graded-again", SampleEval.Beta, 1, EvalRunStatus.Done, T0.AddDays(3),
            Total: new Total(0m, "USD", 0, 0), RescoreOf: "live"));
        await _store.SaveConversationAsync("graded-again",
            Graded(SampleMode.Beta, 1, SampleScenario.Alpha, 1, TrialOutcome.Fail));

        var points = (await EvalTrends.ComputeAsync(_store, SampleEval.Beta)).Modes.Single().Points;

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
        await Run("b", "cook", hour: 2, repeat: 1, EvalRunStatus.Done, cookCost: 0.05m, passed: 4);
        await Run("a", "cook", hour: 1, repeat: 1, EvalRunStatus.Done, cookCost: 0.04m, passed: 3);
        await Run("live", "cook", hour: 3, repeat: 1, EvalRunStatus.Running, cookCost: 0.09m, passed: 4);
        await Run("other", "routing", hour: 1, repeat: 1, EvalRunStatus.Done, cookCost: 1m, passed: 4);

        var trend = await EvalTrends.ComputeAsync(_store, "cook");

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
        await Run("a", "cook", hour: 1, repeat: 1, EvalRunStatus.Done, cookCost: 0.04m, passed: 3,
            models: [new ModelUse(SampleMode.Beta, ModelLevel.Agent, SampleRole.Alpha, "claude-haiku-5-5")]);
        await Run("b", "cook", hour: 2, repeat: 1, EvalRunStatus.Done, cookCost: 0.05m, passed: 3,
            models: [new ModelUse(SampleMode.Beta, ModelLevel.Agent, SampleRole.Alpha, "claude-sonnet-5-5")]);

        var trend = await EvalTrends.ComputeAsync(_store, "cook");

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

    /// <summary>A point's score is the share of the mode's conversations that passed.</summary>
    [Test]
    public async Task TheScore_IsTheSharePassed_PerMode()
    {
        // Order: the alpha mode's two conversations (1, 2), then the beta mode's (3, 4); the first three pass.
        await Run("a", "cook", hour: 1, repeat: 1, EvalRunStatus.Done, cookCost: 0.04m, passed: 3);

        var trend = await EvalTrends.ComputeAsync(_store, "cook");

        Assert.That(trend.Modes.Single(m => m.Name == SampleMode.Alpha).Points.Single().Score, Is.EqualTo(1.0));
        Assert.That(trend.Modes.Single(m => m.Name == SampleMode.Beta).Points.Single().Score, Is.EqualTo(0.5));
        Assert.That(trend.Modes.Single(m => m.Name == SampleMode.Beta).Points.Single().Conversations, Is.EqualTo(2));
    }

    /// <summary>
    /// A conversation our side broke, and one not graded yet, stay out of the score, as pass@k leaves them out: they
    /// are neither a pass nor a fail.
    /// </summary>
    [Test]
    public async Task AnInfrastructureFailure_AndAnUngradedConversation_StayOutOfTheScore()
    {
        await _store.SaveRunAsync(new Run("a", "cook", 1, EvalRunStatus.Done, T0));
        await _store.SaveConversationAsync("a", Graded(SampleMode.Beta, 1, "pass", 1, TrialOutcome.Pass));
        await _store.SaveConversationAsync("a", Graded(SampleMode.Beta, 1, "broke", 2, TrialOutcome.Infrastructure));
        await _store.SaveConversationAsync("a",
            new Conversation(new ConversationKey(SampleMode.Beta, 1, "open"), 3, null, null, 0, 0));

        var point = (await EvalTrends.ComputeAsync(_store, "cook")).Modes.Single().Points.Single();

        Assert.That((point.Score, point.Conversations), Is.EqualTo(((double?)1.0, 3)));
    }

    /// <summary>A run of several repeats shows what one repeat cost.</summary>
    [Test]
    public async Task ARunOfSeveralRepeats_IsWhatOneRepeatCost()
    {
        await Run("a", "cook", hour: 1, repeat: 2, EvalRunStatus.Done, cookCost: 0.04m, passed: 8);

        var point = (await EvalTrends.ComputeAsync(_store, "cook")).Modes.Single(m => m.Name == SampleMode.Beta)
            .Points.Single();

        Assert.That(point.Cost, Is.EqualTo(0.08m), "four conversations over two repeats");
        Assert.That(point.Conversations, Is.EqualTo(4));
    }

    /// <summary>A run where nothing is priced has an unknown cost, not zero.</summary>
    [Test]
    public async Task ARunWithNoPricedRequest_HasAnUnknownCost_NotZero()
    {
        await Run("a", "cook", hour: 1, repeat: 1, EvalRunStatus.Done, cookCost: null, passed: 4);

        var point = (await EvalTrends.ComputeAsync(_store, "cook")).Modes.Single(m => m.Name == SampleMode.Beta)
            .Points.Single();

        Assert.That(point.Cost, Is.Null);
    }

    /// <summary>
    /// The trend has a line for every measure: tokens, time, cache, tools, value and sites. The share priced with a
    /// source is unknown now, since no grader measures it.
    /// </summary>
    [Test]
    public async Task EveryMeasureHasItsLine_TheTokensTimeCacheToolsValueAndSites()
    {
        // One run, one mode, two conversations: one passed, one failed.
        await _store.SaveRunAsync(new Run("a", "cook", 1, EvalRunStatus.Done, T0));
        await _store.SaveConversationAsync("a", Graded(SampleMode.Beta, 1, SampleScenario.Alpha, 1, TrialOutcome.Pass));
        await _store.SaveConversationAsync("a", Graded(SampleMode.Beta, 1, SampleScenario.Beta, 2, TrialOutcome.Fail));

        // Each conversation has one turn reading two sites (one a shop); the first turn's search is refused once of
        // two calls.
        await _store.SaveTurnAsync("a", new Turn(new TurnKey(SampleMode.Beta, 1, SampleScenario.Alpha, 1), "s1", "?",
            "!", SampleRole.Alpha, null, 1, 2,
            ["checkers.co.za", "food.com"], 10, null), [Use("u1"), Refused("u1"), Use("u2")]);
        await _store.SaveTurnAsync("a", new Turn(new TurnKey(SampleMode.Beta, 1, SampleScenario.Beta, 1), "s2", "?",
            "!", SampleRole.Alpha, null, 1, 0,
            ["checkers.co.za"], 20, null), []);

        // One request per conversation, with its cost and its cached input.
        await _store.AddRequestsAsync("a",
            [Priced(SampleScenario.Alpha, 0.06m, 200, 700), Priced(SampleScenario.Beta, 0.02m, 100, 0)]);

        var point = (await EvalTrends.ComputeAsync(_store, "cook")).Modes.Single().Points.Single();

        Assert.That(point.Tokens, Is.EqualTo(1200L), "input, output and cache, per repeat");
        Assert.That(point.SecondsPerTurn, Is.EqualTo(15.0));
        Assert.That(point.CacheShare, Is.EqualTo(0.7).Within(1e-9), "700 of the 1,000 input tokens from the cache");
        Assert.That(point.Sourced, Is.Null, "no grader measures it");
        Assert.That(point.ToolsFailed, Is.EqualTo(0.5), "one of two tool calls refused");
        Assert.That(point.CostPerAnswer, Is.EqualTo(0.06m),
            SampleScenario.Fill("one passed conversation, <ScenarioAlpha>'s cost"));
        Assert.That(point.Sites, Is.EqualTo(2));
    }

    /// <summary>With a limit, the trend shows only that many of the latest runs.</summary>
    [Test]
    public async Task OnlyTheLastRuns_UpToTheLimit()
    {
        for (int h = 1; h <= 5; h++)
            await Run($"r{h}", "cook", hour: h, repeat: 1, EvalRunStatus.Done, cookCost: 0.04m, passed: 4);

        var trend = await EvalTrends.ComputeAsync(_store, "cook", limit: 3);

        Assert.That(trend.Modes[0].Points.Select(p => p.Run), Is.EqualTo(new[] { "r3", "r4", "r5" }));
    }

    /// <summary>An eval with no runs has no lines.</summary>
    [Test]
    public async Task ACheckWithoutRuns_HasNoModes() =>
        Assert.That((await EvalTrends.ComputeAsync(_store, "cook")).Modes, Is.Empty);

    /// <summary>A stored stream event: a web search starting.</summary>
    private static StreamEvent Use(string id) =>
        Event($$"""{"content_block":{"type":"tool_use","id":"{{id}}",""" + """ "name":"WebSearch","input":{}}}""");

    /// <summary>A stored stream event: a web search refused by its limit.</summary>
    private static StreamEvent Refused(string id) =>
        Event($$"""{"content_block":{"type":"tool_result","tool_use_id":"{{id}}",""" +
              """ "content":"Error: limit","is_error":true}}""");

    /// <summary>A stored stream event of a block starting, with its data.</summary>
    private static StreamEvent Event(string data) =>
        new("content_block_start", JsonDocument.Parse(data).RootElement.Clone(), null);

    /// <summary>One request of a task's first turn, with its cost and its cached input.</summary>
    private static Request Priced(string task, decimal cost, long input, long cached) =>
        new("a", new TurnKey(SampleMode.Beta, 1, task, 1), T0, "turn", "s", null, "m", input, 100, cached, 0, cost,
            1000);
}
