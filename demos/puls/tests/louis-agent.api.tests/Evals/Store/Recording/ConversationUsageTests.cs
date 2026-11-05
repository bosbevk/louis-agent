using LouisAgent.Api.Evals.Runs;
using LouisAgent.Api.Evals.Store;
using LouisAgent.Api.Evals.Store.Recording;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Core.Usage.Sinks;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Protocol.Events.Conversations;
using LouisAgent.Evals.Protocol.Events.Run;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Protocol.Parts.Conversations;
using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Tests.Evals.Store.Recording;

/// <summary>
/// A conversation's tokens and cost come from the ledger once the run ends: its own requests, summed, and a judge's
/// grading left out. A number the ledger didn't know for any of them leaves the total unknown, never 0.
/// </summary>
public class ConversationUsageTests
{
    /// <summary>What the runner measured: two turns, the tokens and cost not known yet.</summary>
    private static readonly Metrics Measured = new(2, 6, ["www.checkers.co.za"], 2, null, null, 30.5, null);

    /// <summary>One request of the conversation, with its tokens and cost.</summary>
    private static Request Asked(string purpose, long? input, long? output, decimal? cost, long cacheRead = 0) =>
        new("run_1", new TurnKey(SampleMode.Beta, 1, "bobotie", 1), DateTimeOffset.UnixEpoch, purpose, "sess_1", null,
            "claude-haiku-5-5", input, output, cacheRead, 0, cost, 100);

    /// <summary>The agent's requests add up, the cached input counted as input; the runner's numbers stay.</summary>
    [Test]
    public void TheAgentsRequests_AddUp_IntoTheConversationsTokensAndCost()
    {
        var metrics = ConversationUsage.With(Measured,
            [Asked("turn", 100, 20, 0.01m, cacheRead: 400), Asked("turn", 50, 10, 0.02m)]);

        Assert.That((metrics.TokensIn, metrics.TokensOut, metrics.Cost),
            Is.EqualTo(((long?)550, (long?)30, (decimal?)0.03m)));
        Assert.That((metrics.AgentCalls, metrics.Turns, metrics.Seconds),
            Is.EqualTo(((int?)2, (int?)2, (double?)30.5)));
    }

    /// <summary>A judge's grading is the grader's cost, not the conversation's.</summary>
    [Test]
    public void AJudgesGrading_IsLeftOut()
    {
        var metrics = ConversationUsage.With(Measured,
            [Asked("turn", 100, 20, 0.01m), Asked(UsagePurpose.Grading, 900, 90, 0.5m)]);

        Assert.That((metrics.TokensIn, metrics.Cost), Is.EqualTo(((long?)100, (decimal?)0.01m)));
    }

    /// <summary>A price the ledger didn't know leaves the cost unknown; the tokens it knew still count.</summary>
    [Test]
    public void AnUnknownPrice_LeavesTheCostUnknown_NeverZero()
    {
        var metrics = ConversationUsage.With(Measured, [Asked("turn", 100, 20, 0.01m), Asked("turn", 50, 10, null)]);

        Assert.That((metrics.TokensIn, metrics.Cost), Is.EqualTo(((long?)150, (decimal?)null)));
    }

    /// <summary>A conversation with no request of its own keeps what the runner measured.</summary>
    [Test]
    public void NoRequestOfItsOwn_KeepsTheRunnersMetrics() =>
        Assert.That(ConversationUsage.With(Measured, [Asked(UsagePurpose.Grading, 900, 90, 0.5m)]),
            Is.SameAs(Measured));

    /// <summary>
    /// The recorder writes a conversation again once the ledger is joined, with its tokens and cost: the run's own
    /// ledger row on its turn's session.
    /// </summary>
    [Test]
    public async Task TheRecorder_WritesTheConversationAgain_WithItsTokensAndCost()
    {
        string directory = Path.Combine(Path.GetTempPath(), "louis-usage-ledger-" + Guid.NewGuid().ToString("N"));
        try
        {
            // The ledger's row for the conversation's one turn.
            new JsonlUsageSink(directory).Record(new UsageRecord(DateTimeOffset.UtcNow, 1, UsagePurpose.Turn, "api",
                "sess_1", 1, "bobotie", $"run123456789:{SampleMode.Beta}:1", null, "claude-haiku-5-5",
                new UsageTokens(1000, 0, 0, 200, null), 1500, "stop", new UsageCost("USD", 0.25m, "2026-10-10"), null,
                null));

            // A run of one conversation, recorded against that ledger.
            var store = new InMemoryEvalStore();
            var run = new RunningEval("run123456789", "cook", 1);
            var recording = new EvalRecorder(store, ledgerDirectory: directory).RecordAsync(run);
            foreach (string line in OneConversation()) run.Add(line);
            run.Complete(0);
            await recording;

            var metrics = (await store.ConversationsAsync("run123456789")).Single().Metrics!;
            Assert.That((metrics.TokensIn, metrics.TokensOut, metrics.Cost),
                Is.EqualTo(((long?)1000, (long?)200, (decimal?)0.25m)));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>The lines of a run with one conversation of one turn, in session <c>sess_1</c>.</summary>
    private static IEnumerable<string> OneConversation()
    {
        var at = DateTimeOffset.UtcNow;
        EvalEvent[] events =
        [
            new ProtocolEvent(at, ProtocolEvent.CurrentVersion, "cook", 1, "abc", RunSplit.Dev, null, null, [], []),
            new ModeEvent(at, SampleMode.Beta, 1, [], null, null, 1),
            new QuestionEvent(at, SampleMode.Beta, 1, "bobotie", 1, "sess_1", "Maak bobotie"),
            new TurnEvent(at, SampleMode.Beta, 1, "bobotie", 1, "sess_1", "Maak bobotie", "Hier", null, null, 1, 0, [],
                2.0, null, null),
            new ConversationEvent(at, SampleMode.Beta, 1, "bobotie", null, 0, 2.0, [], TrialOutcome.Pass, Measured),
        ];
        return events.Select(EvalProtocolJson.Line);
    }
}
