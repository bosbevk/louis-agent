using LouisAgent.Testing;
using LouisAgent.Puls.Admin.Summary;

namespace LouisAgent.Puls.Tests.Admin.Summary;

// F18 step 5c.3: the words of a roll-up line. A slice with too few trials reads "2 of 3 passed · too few for a rate", never a percentage that looks precise and is not; an unknown cost
// reads "unknown" and a re-score's "$0" (ADR 0007); a cost per pass is a dash where nothing passed. Written first: the words threw NotImplementedException on the red run.
public class SummaryTextTests
{
    private static SliceView Slice(int n = 8, int passed = 6, int infrastructure = 0, bool tooFew = false, double? rate = 0.75, bool hasPass = true, decimal? perPass = 0.13m) =>
        new(SampleMode.Beta, "overall", n, passed, n - passed - infrastructure, 0, infrastructure, ["a"], tooFew, rate, 1, 0.5, 0.1m, hasPass, perPass);

    [Test]
    public void Passed_IsOfTheCountedTrials_NotOfAllOfThem() =>
        Assert.That(SummaryText.Passed(Slice(n: 8, passed: 6, infrastructure: 1)), Is.EqualTo("6 of 7 passed"));

    [Test]
    public void ATooFewSlice_SaysSo_AfterItsCounts() =>
        Assert.That(SummaryText.Passed(Slice(n: 3, passed: 2, tooFew: true, rate: null)), Is.EqualTo("2 of 3 passed · too few for a rate"));

    [Test]
    public void ARate_IsAPercentage_AndATooFewSlicesIsADash()
    {
        Assert.That(SummaryText.Rate(Slice(), 0.75), Is.EqualTo("75%"));
        Assert.That(SummaryText.Rate(Slice(tooFew: true, rate: null), null), Is.EqualTo("–"));
        Assert.That(SummaryText.Rate(Slice(tooFew: true, rate: 0.5), 0.5), Is.EqualTo("–"), "even if a rate were sent for a slice with too few trials");
    }

    [Test]
    public void ACost_IsUnknown_ZeroOrAnAmount()
    {
        Assert.That(SummaryText.Cost(null, null), Is.EqualTo("unknown"));
        Assert.That(SummaryText.Cost(0m, null), Is.EqualTo("$0"));
        Assert.That(SummaryText.Cost(0.012m, null), Does.StartWith("$0.01"));
    }

    [Test]
    public void ACostPerPass_IsADash_WhereNothingPassed()
    {
        Assert.That(SummaryText.CostPerPass(Slice(hasPass: false, perPass: null), null), Is.EqualTo("–"));
        Assert.That(SummaryText.CostPerPass(Slice(hasPass: true, perPass: null), null), Is.EqualTo("unknown"), "passed, but a price was unknown");
    }
}
