using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Puls.Components.Admin.Rates;

namespace LouisAgent.Puls.Tests.Components.Admin.Rates;

/// <summary>
/// The run's page shows each mode's pass@k and pass^k, labelled with k, and a dash for none (F18 step 5a.2).
/// </summary>
public class ModeRatesListTests
{
    /// <summary>The list for these mode rows.</summary>
    private static IRenderedComponent<ModeRatesList> Render(params ModeRow[] modes)
    {
        var ctx = new BunitContext();
        return ctx.Render<ModeRatesList>(p => p.Add(x => x.Modes, modes));
    }

    /// <summary>A mode's row for one repeat.</summary>
    private static ModeRow Row(double? passAtK, double? passHatK, int? k, int? infrastructure, int? retried) =>
        new(SampleMode.Beta, 1, [], null, null, passAtK, passHatK, k, infrastructure, retried);

    /// <summary>Each mode shows its rates labelled with k, and its left-out and retried trials.</summary>
    [Test]
    public void EachMode_ShowsItsRates_LabelledWithK()
    {
        var cut = Render(Row(1, 0.5, 3, 1, 2));

        var item = cut.Find(".mode-rate");
        Assert.That(item.QuerySelector(".mode-rate-name")!.TextContent, Is.EqualTo(SampleMode.Beta));
        Assert.That(item.QuerySelector(".pass-at")!.TextContent, Is.EqualTo("pass@3 100%"));
        Assert.That(item.QuerySelector(".pass-hat")!.TextContent, Is.EqualTo("pass^3 50%"));
        Assert.That(item.QuerySelector(".left-out")!.TextContent, Is.EqualTo("1 left out (infrastructure)"));
        Assert.That(item.QuerySelector(".retried")!.TextContent, Is.EqualTo("2 retried"));
    }

    /// <summary>A rate no trial made is a dash, and nothing left out shows no chip.</summary>
    [Test]
    public void ARateNoTrialMade_IsADash_AndNothingLeftOutShowsNoChip()
    {
        var cut = Render(Row(null, null, 1, 0, 0));

        Assert.That(cut.Find(".pass-at").TextContent, Is.EqualTo("pass@1 –"));
        Assert.That(cut.Find(".pass-hat").TextContent, Is.EqualTo("pass^1 –"));
        Assert.That(cut.FindAll(".left-out"), Is.Empty);
        Assert.That(cut.FindAll(".retried"), Is.Empty);
    }

    /// <summary>A run from before the rates says so instead of showing zeroes.</summary>
    [Test]
    public void ARunFromBeforeTheRates_SaysSoInsteadOfShowingZeroes()
    {
        var cut = Render(Row(null, null, null, null, null));

        Assert.That(cut.FindAll(".mode-rate"), Is.Empty);
        Assert.That(cut.Find(".mode-rates-none").TextContent, Does.Contain("no pass rates"));
    }
}
