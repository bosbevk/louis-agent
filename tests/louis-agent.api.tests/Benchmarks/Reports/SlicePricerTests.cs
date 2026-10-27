using LouisAgent.Testing;
using LouisAgent.Api.Benchmarks.Reports;
using LouisAgent.Api.Benchmarks.Runs;
using static LouisAgent.Api.Benchmarks.Runs.BenchmarkProtocol;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;

namespace LouisAgent.Api.Tests.Benchmarks.Reports;

// F18 step 5c.2: a slice's cost comes from the ledger, never the runner (ADR 0007): the cost of the conversations of the slice's tasks, in its mode, over its trials (cost_per_trial) and over
// its passes (cost_per_pass, absent when nothing passed). An unknown price in any of those conversations makes the slice's cost unknown (null); a re-score asked nothing, so its cost is a
// known 0. The ledger's rows are already joined to their conversations (BenchmarkLedger.Join), specialists and minions included. Written first: SlicePricer threw NotImplementedException.
public class SlicePricerTests
{
    private static readonly Total Known = new(1m, "USD", 1000, 5);

    private static BenchmarkSlice Slice(string[] tasks, int n = 4, int passed = 2, string mode = SampleMode.Beta) =>
        new(mode, "overall", n, passed, n - passed, 0, 0, tasks, TooFew: true, null, null, null, [], [], []);

    private static Request Row(string task, decimal? cost, int repeat = 1, string mode = SampleMode.Beta) =>
        new("run1", new TurnKey(mode, repeat, task, 1), DateTimeOffset.UnixEpoch, "turn", "s", null, "claude-haiku-5-5", 10, 10, 0, 0, cost, 1);

    [Test]
    public void TheCost_IsTheSumOfTheSlicesConversations_OverItsTrials_AndItsPasses()
    {
        var cost = SlicePricer.Of(Slice(["a", "b"]), Known, [Row("a", 0.25m), Row("a", 0.25m, repeat: 2), Row("b", 0.5m), Row("c", 9m)]);

        Assert.That(cost.PerTrial, Is.EqualTo(1m / 4), "a and b, both repeats, 1.00 over 4 trials; c is not in the slice");
        Assert.That(cost.PerPass, Is.EqualTo(1m / 2));
    }

    [Test]
    public void AnotherModesConversationOfTheSameTask_IsNotInTheSlice()
    {
        var cost = SlicePricer.Of(Slice(["a"], n: 1, passed: 1), Known, [Row("a", 0.2m), Row("a", 7m, mode: SampleMode.Gamma)]);

        Assert.That(cost.PerTrial, Is.EqualTo(0.2m));
    }

    [Test]
    public void ARowWithAnUnknownPrice_MakesTheSlicesCostUnknown_NullNeverZero()
    {
        var cost = SlicePricer.Of(Slice(["a", "b"]), Known, [Row("a", 0.25m), Row("b", null)]);

        Assert.That((cost.PerTrial, cost.PerPass), Is.EqualTo(((decimal?)null, (decimal?)null)));
    }

    [Test]
    public void ARowOfAnotherSlice_WithAnUnknownPrice_DoesNotSpoilThisOne() =>
        Assert.That(SlicePricer.Of(Slice(["a"], n: 2, passed: 2), Known, [Row("a", 0.4m), Row("z", null)]).PerTrial, Is.EqualTo(0.2m));

    // Review of 5c.2: a ledger row that can't be placed on a conversation still counts in the run's total, so a slice priced from the rows that were placed would be "a sum of the rest" and
    // come out lower than what the mode really cost. A run with such a row prices no slice: unknown, as an unknown price does (ADR 0007).
    [Test]
    public void ARowNobodyCouldPlaceOnAConversation_MakesEverySlicesCostUnknown()
    {
        var unplaced = Row("a", 0.3m) with { Turn = null };

        var cost = SlicePricer.Of(Slice(["a"], n: 2, passed: 2), Known, [Row("a", 0.4m), unplaced]);

        Assert.That((cost.PerTrial, cost.PerPass), Is.EqualTo(((decimal?)null, (decimal?)null)));
    }

    [Test]
    public void AnUnplacedRowWithNoPrice_AlsoMakesTheCostUnknown() =>
        Assert.That(SlicePricer.Of(Slice(["a"]), Known, [Row("a", 0.4m), Row("a", null) with { Turn = null }]).PerTrial, Is.Null);

    [Test]
    public void WhenNothingPassed_ThereIsNoCostPerPass()
    {
        var cost = SlicePricer.Of(Slice(["a"], n: 2, passed: 0), Known, [Row("a", 0.4m)]);

        Assert.That((cost.PerTrial, cost.PerPass), Is.EqualTo(((decimal?)0.2m, (decimal?)null)));
    }

    [Test]
    public void AReScore_CostsAKnownZero_BecauseItsTotalIs()
    {
        var cost = SlicePricer.Of(Slice(["a", "b"]), new Total(0m, "USD", 0, 0), []);

        Assert.That((cost.PerTrial, cost.PerPass), Is.EqualTo(((decimal?)0m, (decimal?)0m)));
    }

    [Test]
    public void ARunWhoseCostIsUnknown_HasNoSliceCost() =>
        Assert.That(SlicePricer.Of(Slice(["a"]), null, []).PerTrial, Is.Null);

    [Test]
    public void ASliceOfNoTrials_HasNoCostPerTrial() =>
        Assert.That(SlicePricer.Of(Slice([], n: 0, passed: 0), Known, []).PerTrial, Is.Null);
}
