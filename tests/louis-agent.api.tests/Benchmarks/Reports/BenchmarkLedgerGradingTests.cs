using LouisAgent.Api.Benchmarks.Reports;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Core.Usage.Records;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;
using static LouisAgent.Api.Benchmarks.Runs.BenchmarkProtocol;

namespace LouisAgent.Api.Tests.Benchmarks.Reports;

// F18 step 6.1: a model grader's requests are in the ledger as `grading`, tagged with the run's id alone (it has no session and no turn). The API's join places them on the
// run, not on a turn, and the run's total includes them; a grading row with no price makes the total unknown, as any request without one does (ADR 0007).
public class BenchmarkLedgerGradingTests
{
    private const string Run = "run123456789";
    private static readonly DateTimeOffset T0 = new(2026, 10, 26, 9, 0, 0, TimeSpan.Zero);
    private static readonly IReadOnlyDictionary<string, ConversationKey> Sessions =
        new Dictionary<string, ConversationKey> { ["sess_puls"] = new("puls-cook", 1, "bobotie") };

    private static UsageRecord Row(int second, string purpose, string? session, int? turn, string? run, decimal? cost) =>
        new(T0.AddSeconds(second), 1, purpose, "api", session, turn, null, run, null, "qwen3:8b", new UsageTokens(1000, 0, 0, 200, null), 1500, "stop",
            cost is null ? null : new UsageCost("USD", cost.Value, "2026-10-10"));

    [Test]
    public void AGradingRow_IsTheRunsWithNoTurn_AndItsCostIsInTheTotal()
    {
        UsageRecord[] ledger = [Row(1, UsagePurpose.Turn, "sess_puls", 1, Run + ":puls-cook:1", 0.01m), Row(2, UsagePurpose.Grading, null, null, Run, 0.002m)];

        var ofRun = BenchmarkLedger.OfRun(Run, ledger);
        var requests = BenchmarkLedger.Join(Run, ofRun, Sessions);

        Assert.That(ofRun, Has.Count.EqualTo(2), "tagged with the run's id alone, it is the run's");
        Assert.That(requests.Single(r => r.Purpose == UsagePurpose.Grading).Turn, Is.Null, "placed on the run, not on a turn");
        Assert.That(requests.Single(r => r.Purpose == UsagePurpose.Turn).Turn, Is.Not.Null);
        Assert.That(BenchmarkLedger.TotalOf(ofRun).Cost, Is.EqualTo(0.012m));
        Assert.That(BenchmarkLedger.TotalOf(ofRun).Requests, Is.EqualTo(2));
    }

    [Test]
    public void AGradingRowWithNoPrice_IsAnUnknownCost_NotAZero_AndTheRunWithNothingElsePricedIsUnknown()
    {
        var ofRun = BenchmarkLedger.OfRun(Run, [Row(2, UsagePurpose.Grading, null, null, Run, null)]);

        var total = BenchmarkLedger.TotalOf(ofRun);

        Assert.That(BenchmarkLedger.Join(Run, ofRun, Sessions).Single().Cost, Is.Null, "the request's own cost stays unknown");
        Assert.That(total.Cost, Is.Null, "no priced request, so no cost: never 0 (ADR 0007)");
        Assert.That(total.Requests, Is.EqualTo(1));
    }

    // The ledger's rule for every total (UsageReport.Total): the cost is the priced requests' sum and the unpriced ones are counted apart, so a run is unknown only when none
    // of its requests is priced. A grading row follows it like any other; whether the API's Total should say "partly unknown" is a question for Louis, not this step.
    [Test]
    public void AGradingRowWithNoPrice_BesideAPricedTurn_FollowsTheLedgersRule_TheCostIsThePricedSum()
    {
        UsageRecord[] ledger = [Row(1, UsagePurpose.Turn, "sess_puls", 1, Run + ":puls-cook:1", 0.01m), Row(2, UsagePurpose.Grading, null, null, Run, null)];

        var total = BenchmarkLedger.TotalOf(BenchmarkLedger.OfRun(Run, ledger));

        Assert.That((total.Cost, total.Requests), Is.EqualTo((0.01m, 2)));
    }
}
