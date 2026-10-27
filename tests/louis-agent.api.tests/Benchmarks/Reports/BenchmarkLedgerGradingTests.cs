using LouisAgent.Testing;
using LouisAgent.Api.Benchmarks.Reports;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Core.Usage.Records;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;
using static LouisAgent.Api.Benchmarks.Runs.BenchmarkProtocol;

namespace LouisAgent.Api.Tests.Benchmarks.Reports;

// F18 step 6.1b: a model grader's requests are ledger rows with the purpose `grading`, tagged as the turns are ({run}:{mode}:{repeat}, plus the scenario) and carrying the
// conversation's session and last turn, so the join places each on its conversation, the run's total counts it, and a slice's cost includes the judging. Tagged with the run alone
// (6.1) they had no turn, and a row no conversation holds made every slice's cost unknown. The ledger's rule for a total is unchanged: unpriced requests are counted apart.
public class BenchmarkLedgerGradingTests
{
    private const string Run = "run123456789";
    private static readonly DateTimeOffset T0 = new(2026, 10, 26, 9, 0, 0, TimeSpan.Zero);

    private static readonly IReadOnlyDictionary<string, ConversationKey> Sessions =
        new Dictionary<string, ConversationKey> { ["sess_puls"] = new(SampleMode.Beta, 1, "bobotie") };

    private static UsageRecord Row(int second, string purpose, string? session, int? turn, string? run, decimal? cost,
        string? task = null) =>
        new(T0.AddSeconds(second), 1, purpose, "api", session, turn, task, run, null, "qwen3:8b",
            new UsageTokens(1000, 0, 0, 200, null), 1500, "stop",
            cost is null ? null : new UsageCost("USD", cost.Value, "2026-10-10"));

    private static UsageRecord Turn(decimal cost) =>
        Row(1, UsagePurpose.Turn, "sess_puls", 1, Run + ":puls-cook:1", cost);

    private static UsageRecord Judge(decimal? cost) => Row(2, UsagePurpose.Grading, "sess_puls", 1,
        Run + ":puls-cook:1", cost, task: "bobotie");

    private static BenchmarkSlice Slice(int n = 1, int passed = 1) =>
        new(SampleMode.Beta, "overall", n, passed, n - passed, 0, 0, ["bobotie"], TooFew: true, null, null, null, [],
            [], []);

    [Test]
    public void AGradingRow_LandsOnItsConversationsTurn_AndItsCostIsInTheRunsTotal()
    {
        var ofRun = BenchmarkLedger.OfRun(Run, [Turn(0.01m), Judge(0.002m)]);

        var requests = BenchmarkLedger.Join(Run, ofRun, Sessions);

        Assert.That(requests.Select(r => r.Turn!.Task), Is.EqualTo(new[] { "bobotie", "bobotie" }),
            "both on the conversation");
        Assert.That(requests.Single(r => r.Purpose == UsagePurpose.Grading).Turn,
            Is.EqualTo(new TurnKey(SampleMode.Beta, 1, "bobotie", 1)));
        Assert.That(BenchmarkLedger.TotalOf(ofRun).Cost, Is.EqualTo(0.012m));
        Assert.That(BenchmarkLedger.TotalOf(ofRun).Requests, Is.EqualTo(2));
    }

    [Test]
    public void ASlicesCost_IncludesTheJudging_AndStaysKnown()
    {
        var ofRun = BenchmarkLedger.OfRun(Run, [Turn(0.01m), Judge(0.002m)]);

        var cost = SlicePricer.Of(Slice(), BenchmarkLedger.TotalOf(ofRun), BenchmarkLedger.Join(Run, ofRun, Sessions));

        Assert.That(cost.PerTrial, Is.EqualTo(0.012m), "the answer and the judging");
    }

    [Test]
    public void AGradingRowWithNoPrice_IsAnUnknownCost_AndSoIsTheSlicesThatItBelongsTo()
    {
        var ofRun = BenchmarkLedger.OfRun(Run, [Turn(0.01m), Judge(null)]);

        var requests = BenchmarkLedger.Join(Run, ofRun, Sessions);

        Assert.That(requests.Single(r => r.Purpose == UsagePurpose.Grading).Cost, Is.Null,
            "its own cost stays unknown, never 0");
        Assert.That(SlicePricer.Of(Slice(), BenchmarkLedger.TotalOf(ofRun), requests).PerTrial, Is.Null);
    }

    // The ledger's rule for every total (UsageReport.Total): the cost is the priced requests' sum and the unpriced ones are counted apart, so a run is unknown only when none of its
    // requests is priced. A grading row follows it like any other.
    [Test]
    public void TheRunsTotal_FollowsTheLedgersRule_ForAGradingRowWithNoPrice()
    {
        Assert.That(BenchmarkLedger.TotalOf(BenchmarkLedger.OfRun(Run, [Judge(null)])).Cost, Is.Null,
            "no priced request, so no cost: never 0 (ADR 0007)");
        Assert.That(BenchmarkLedger.TotalOf(BenchmarkLedger.OfRun(Run, [Turn(0.01m), Judge(null)])).Cost,
            Is.EqualTo(0.01m), "the priced sum");
    }
}
