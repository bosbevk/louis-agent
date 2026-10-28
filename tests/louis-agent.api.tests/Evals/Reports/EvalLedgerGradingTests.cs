using LouisAgent.Api.Evals.Reports;
using LouisAgent.Api.Evals.Runs.Protocol;
using LouisAgent.Core.Usage.Records;
using static LouisAgent.Api.Evals.Runs.Protocol.EvalProtocol;

namespace LouisAgent.Api.Tests.Evals.Reports;

/// <summary>
/// When a model grades a conversation, its requests land in the ledger tagged like the turns, with the
/// conversation's session and last turn. So the join puts each on its conversation, the run's total counts it, and a
/// slice's cost includes the judging. Unpriced requests are still counted apart, as for any total.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0007-measure-every-request.md">ADR 0007: measure every request</seealso>
public class EvalLedgerGradingTests
{
    /// <summary>The id of the run the rows belong to.</summary>
    private const string Run = "run123456789";

    /// <summary>When the first row was written.</summary>
    private static readonly DateTimeOffset T0 = new(2026, 10, 26, 9, 0, 0, TimeSpan.Zero);

    /// <summary>The one conversation of the run, found by its session.</summary>
    private static readonly IReadOnlyDictionary<string, ConversationKey> Sessions =
        new Dictionary<string, ConversationKey> { ["sess_puls"] = new(SampleMode.Beta, 1, "bobotie") };

    /// <summary>A ledger row for one request, with or without a price.</summary>
    private static UsageRecord Row(int second, string purpose, string? session, int? turn, string? run, decimal? cost,
        string? task = null) =>
        new(T0.AddSeconds(second), 1, purpose, "api", session, turn, task, run, null, "qwen3:8b",
            new UsageTokens(1000, 0, 0, 200, null), 1500, "stop",
            cost is null ? null : new UsageCost("USD", cost.Value, "2026-10-10"));

    /// <summary>The conversation's one answering turn, at that price.</summary>
    private static UsageRecord Turn(decimal cost) =>
        Row(1, UsagePurpose.Turn, "sess_puls", 1, Run + ":puls-cook:1", cost);

    /// <summary>The judge's request on the same conversation, priced or not.</summary>
    private static UsageRecord Judge(decimal? cost) => Row(2, UsagePurpose.Grading, "sess_puls", 1,
        Run + ":puls-cook:1", cost, task: "bobotie");

    /// <summary>The run's overall slice, with one trial by default.</summary>
    private static EvalSlice Slice(int n = 1, int passed = 1) =>
        new(SampleMode.Beta, "overall", n, passed, n - passed, 0, 0, ["bobotie"], TooFew: true, null, null, null, [],
            [], []);

    /// <summary>The judge's row sits on the conversation's turn, and its cost is part of the run's total.</summary>
    [Test]
    public void AGradingRow_LandsOnItsConversationsTurn_AndItsCostIsInTheRunsTotal()
    {
        var ofRun = EvalLedger.OfRun(Run, [Turn(0.01m), Judge(0.002m)]);

        var requests = EvalLedger.Join(Run, ofRun, Sessions);

        Assert.That(requests.Select(r => r.Turn!.Task), Is.EqualTo(new[] { "bobotie", "bobotie" }),
            "both on the conversation");
        Assert.That(requests.Single(r => r.Purpose == UsagePurpose.Grading).Turn,
            Is.EqualTo(new TurnKey(SampleMode.Beta, 1, "bobotie", 1)));
        Assert.That(EvalLedger.TotalOf(ofRun).Cost, Is.EqualTo(0.012m));
        Assert.That(EvalLedger.TotalOf(ofRun).Requests, Is.EqualTo(2));
    }

    /// <summary>A slice's cost counts the judging too, and is still known.</summary>
    [Test]
    public void ASlicesCost_IncludesTheJudging_AndStaysKnown()
    {
        var ofRun = EvalLedger.OfRun(Run, [Turn(0.01m), Judge(0.002m)]);

        var cost = SlicePricer.Of(Slice(), EvalLedger.TotalOf(ofRun), EvalLedger.Join(Run, ofRun, Sessions));

        Assert.That(cost.PerTrial, Is.EqualTo(0.012m), "the answer and the judging");
    }

    /// <summary>A judge's request with no price has an unknown cost, and so does its slice.</summary>
    [Test]
    public void AGradingRowWithNoPrice_IsAnUnknownCost_AndSoIsTheSlicesThatItBelongsTo()
    {
        var ofRun = EvalLedger.OfRun(Run, [Turn(0.01m), Judge(null)]);

        var requests = EvalLedger.Join(Run, ofRun, Sessions);

        Assert.That(requests.Single(r => r.Purpose == UsagePurpose.Grading).Cost, Is.Null,
            "its own cost stays unknown, never 0");
        Assert.That(SlicePricer.Of(Slice(), EvalLedger.TotalOf(ofRun), requests).PerTrial, Is.Null);
    }

    /// <summary>
    /// A judge's unpriced request follows the ledger's rule for every total: the cost is the sum of what's priced, so
    /// a run's cost is unknown only when nothing in it is priced.
    /// </summary>
    [Test]
    public void TheRunsTotal_FollowsTheLedgersRule_ForAGradingRowWithNoPrice()
    {
        Assert.That(EvalLedger.TotalOf(EvalLedger.OfRun(Run, [Judge(null)])).Cost, Is.Null,
            "no priced request, so no cost: never 0 (ADR 0007)");
        Assert.That(EvalLedger.TotalOf(EvalLedger.OfRun(Run, [Turn(0.01m), Judge(null)])).Cost,
            Is.EqualTo(0.01m), "the priced sum");
    }
}
