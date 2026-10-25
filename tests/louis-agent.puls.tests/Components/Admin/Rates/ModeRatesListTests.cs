using System.Text.Json;
using LouisAgent.Puls.Components.Admin.Rates;

namespace LouisAgent.Puls.Tests.Components.Admin.Rates;

// F18 step 5a.2: the run's page shows each mode's pass@k and pass^k, labelled with k, and a dash for none. Written first: the component was empty.
public class ModeRatesListTests
{
    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    private static IRenderedComponent<ModeRatesList> Render(string run)
    {
        var ctx = new BunitContext();
        return ctx.Render<ModeRatesList>(p => p.Add(x => x.Run, Json(run)));
    }

    [Test]
    public void EachMode_ShowsItsRates_LabelledWithK()
    {
        var cut = Render("""
            {"modes":[{"name":"puls-cook","repeat":3,"pass_at_k":1,"pass_hat_k":0.5,"k":3,"infrastructure":1,"retried":2}]}
            """);

        var item = cut.Find(".mode-rate");
        Assert.That(item.QuerySelector(".mode-rate-name")!.TextContent, Is.EqualTo("puls-cook"));
        Assert.That(item.QuerySelector(".pass-at")!.TextContent, Is.EqualTo("pass@3 100%"));
        Assert.That(item.QuerySelector(".pass-hat")!.TextContent, Is.EqualTo("pass^3 50%"));
        Assert.That(item.QuerySelector(".left-out")!.TextContent, Is.EqualTo("1 left out (infrastructure)"));
        Assert.That(item.QuerySelector(".retried")!.TextContent, Is.EqualTo("2 retried"));
    }

    [Test]
    public void ARateNoTrialMade_IsADash_AndNothingLeftOutShowsNoChip()
    {
        var cut = Render("""{"modes":[{"name":"puls-cook","repeat":1,"pass_at_k":null,"pass_hat_k":null,"k":1,"infrastructure":0,"retried":0}]}""");

        Assert.That(cut.Find(".pass-at").TextContent, Is.EqualTo("pass@1 –"));
        Assert.That(cut.Find(".pass-hat").TextContent, Is.EqualTo("pass^1 –"));
        Assert.That(cut.FindAll(".left-out"), Is.Empty);
        Assert.That(cut.FindAll(".retried"), Is.Empty);
    }

    [Test]
    public void ARunFromBeforeTheRates_SaysSoInsteadOfShowingZeroes()
    {
        var cut = Render("""{"modes":[{"name":"puls-cook","repeat":1,"toolsets":["web"]}]}""");

        Assert.That(cut.FindAll(".mode-rate"), Is.Empty);
        Assert.That(cut.Find(".mode-rates-none").TextContent, Does.Contain("no pass rates"));
    }
}
