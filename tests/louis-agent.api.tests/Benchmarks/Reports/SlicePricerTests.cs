using LouisAgent.Testing;
using LouisAgent.Api.Benchmarks.Reports;
using LouisAgent.Api.Benchmarks.Runs;
using static LouisAgent.Api.Benchmarks.Runs.BenchmarkProtocol;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;

namespace LouisAgent.Api.Tests.Benchmarks.Reports;

/// <summary>
/// A slice's cost comes from the ledger, never from the runner: what its tasks' conversations in its mode cost, per
/// trial and per pass. One unknown price makes the slice's cost unknown; a re-score asked nothing, so it costs a
/// known 0. The ledger rows arrive already placed on their conversations, specialists and minions included.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0007-measure-every-request.md">ADR 0007: measure every request</seealso>
public class SlicePricerTests
{
    /// <summary>A run's total with a known cost.</summary>
    private static readonly Total Known = new(1m, "USD", 1000, 5);

    /// <summary>The overall slice over these tasks, four trials with two passed by default.</summary>
    private static BenchmarkSlice Slice(string[] tasks, int n = 4, int passed = 2, string mode = SampleMode.Beta) =>
        new(mode, "overall", n, passed, n - passed, 0, 0, tasks, TooFew: true, null, null, null, [], [], []);

    /// <summary>A ledger row placed on a task's first turn, priced or not.</summary>
    private static Request Row(string task, decimal? cost, int repeat = 1, string mode = SampleMode.Beta) =>
        new("run1", new TurnKey(mode, repeat, task, 1), DateTimeOffset.UnixEpoch, "turn", "s", null, "claude-haiku-5-5",
            10, 10, 0, 0, cost, 1);

    /// <summary>The cost adds up the slice's conversations, then splits it over its trials and its passes.</summary>
    [Test]
    public void TheCost_IsTheSumOfTheSlicesConversations_OverItsTrials_AndItsPasses()
    {
        var cost = SlicePricer.Of(Slice(["a", "b"]), Known,
            [Row("a", 0.25m), Row("a", 0.25m, repeat: 2), Row("b", 0.5m), Row("c", 9m)]);

        Assert.That(cost.PerTrial, Is.EqualTo(1m / 4),
            "a and b, both repeats, 1.00 over 4 trials; c is not in the slice");
        Assert.That(cost.PerPass, Is.EqualTo(1m / 2));
    }

    /// <summary>The same task in another mode doesn't count toward this slice.</summary>
    [Test]
    public void AnotherModesConversationOfTheSameTask_IsNotInTheSlice()
    {
        var cost = SlicePricer.Of(Slice(["a"], n: 1, passed: 1), Known,
            [Row("a", 0.2m), Row("a", 7m, mode: SampleMode.Gamma)]);

        Assert.That(cost.PerTrial, Is.EqualTo(0.2m));
    }

    /// <summary>One unpriced row in the slice makes its cost unknown, not zero.</summary>
    [Test]
    public void ARowWithAnUnknownPrice_MakesTheSlicesCostUnknown_NullNeverZero()
    {
        var cost = SlicePricer.Of(Slice(["a", "b"]), Known, [Row("a", 0.25m), Row("b", null)]);

        Assert.That((cost.PerTrial, cost.PerPass), Is.EqualTo(((decimal?)null, (decimal?)null)));
    }

    /// <summary>An unpriced row from another slice leaves this slice's cost alone.</summary>
    [Test]
    public void ARowOfAnotherSlice_WithAnUnknownPrice_DoesNotSpoilThisOne() =>
        Assert.That(SlicePricer.Of(Slice(["a"], n: 2, passed: 2), Known, [Row("a", 0.4m), Row("z", null)]).PerTrial,
            Is.EqualTo(0.2m));

    /// <summary>
    /// A row we couldn't place on a conversation still counts in the run's total, so pricing a slice from the rest
    /// would come out too low. Such a run prices no slice at all.
    /// </summary>
    [Test]
    public void ARowNobodyCouldPlaceOnAConversation_MakesEverySlicesCostUnknown()
    {
        var unplaced = Row("a", 0.3m) with { Turn = null };

        var cost = SlicePricer.Of(Slice(["a"], n: 2, passed: 2), Known, [Row("a", 0.4m), unplaced]);

        Assert.That((cost.PerTrial, cost.PerPass), Is.EqualTo(((decimal?)null, (decimal?)null)));
    }

    /// <summary>An unplaced row with no price makes the cost unknown too.</summary>
    [Test]
    public void AnUnplacedRowWithNoPrice_AlsoMakesTheCostUnknown() =>
        Assert.That(SlicePricer.Of(Slice(["a"]), Known, [Row("a", 0.4m), Row("a", null) with { Turn = null }]).PerTrial,
            Is.Null);

    /// <summary>When nothing passed there's a cost per trial but none per pass.</summary>
    [Test]
    public void WhenNothingPassed_ThereIsNoCostPerPass()
    {
        var cost = SlicePricer.Of(Slice(["a"], n: 2, passed: 0), Known, [Row("a", 0.4m)]);

        Assert.That((cost.PerTrial, cost.PerPass), Is.EqualTo(((decimal?)0.2m, (decimal?)null)));
    }

    /// <summary>A re-score costs a known zero, because its total is zero.</summary>
    [Test]
    public void AReScore_CostsAKnownZero_BecauseItsTotalIs()
    {
        var cost = SlicePricer.Of(Slice(["a", "b"]), new Total(0m, "USD", 0, 0), []);

        Assert.That((cost.PerTrial, cost.PerPass), Is.EqualTo(((decimal?)0m, (decimal?)0m)));
    }

    /// <summary>A run whose cost is unknown gives its slices no cost.</summary>
    [Test]
    public void ARunWhoseCostIsUnknown_HasNoSliceCost() =>
        Assert.That(SlicePricer.Of(Slice(["a"]), null, []).PerTrial, Is.Null);

    /// <summary>A slice with no trials has no cost per trial.</summary>
    [Test]
    public void ASliceOfNoTrials_HasNoCostPerTrial() =>
        Assert.That(SlicePricer.Of(Slice([], n: 0, passed: 0), Known, []).PerTrial, Is.Null);
}
