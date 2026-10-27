using System.Text.Json;
using LouisAgent.Puls.Components.Admin;
using LouisAgent.Puls.Components.Admin.Summary;

namespace LouisAgent.Puls.Tests.Components.Admin.Summary;

// F18 step 5c.4: the trend per category per model: a chart for each category, a line per mode on its model, a point per run that had a rate, drawn with the trend chart showing only pass@k
// and pass^k. A slice with too few trials has no rate, so it is a gap in the line (the API sends no point), never a 0. Written first: the panels filter and the component did not exist.
public class CategoryTrendsTests
{
    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    private const string Trend = """
                                 {"check":"puls-cook","modes":[],"categories":[
                                   {"category":"language:af","modes":[
                                     {"name":"puls-cook (claude-haiku-5-5)","model":"claude-haiku-5-5","points":[
                                       {"run":"r1","at":"2026-10-24T09:00:00+00:00","pass_rate":0.5,"pass_at_k":0.5,"pass_hat_k":0.25,"n":8},
                                       {"run":"r3","at":"2026-10-26T09:00:00+00:00","pass_rate":0.75,"pass_at_k":0.75,"pass_hat_k":0.5,"n":8}]},
                                     {"name":"puls-tools","model":null,"points":[
                                       {"run":"r2","at":"2026-10-25T09:00:00+00:00","pass_rate":0.1,"pass_at_k":0.1,"pass_hat_k":0.1,"n":8}]}]},
                                   {"category":"overall","modes":[
                                     {"name":"puls-cook (claude-haiku-5-5)","model":"claude-haiku-5-5","points":[
                                       {"run":"r1","at":"2026-10-24T09:00:00+00:00","pass_rate":0.5,"pass_at_k":0.5,"pass_hat_k":0.25,"n":16}]}]}]}
                                 """;

    private static IRenderedComponent<CategoryTrends> Render(string trend)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        return ctx.Render<CategoryTrends>(p => p.Add(x => x.Trend, Json(trend)));
    }

    // Reworked 2026-10-26 (Louis: "I need to know what it tells me"): a card per category, with the latest run's pass@k and pass^k per mode as bars and numbers; the trend lines only once
    // a line has two runs, else the card says there is no trend yet instead of drawing a lone dot.
    [Test]
    public void EachCategory_IsACard_WithTheLatestRatesPerMode_AsNumbers()
    {
        var cut = Render(Trend);

        var cards = cut.FindAll(".category-card");
        Assert.That(cards.Select(c => c.QuerySelector("h4 code")!.TextContent),
            Is.EqualTo(new[] { "language:af", "overall" }));
        var af = cards[0];
        Assert.That(af.QuerySelector("h4 .muted")!.TextContent, Is.EqualTo("3 runs"));
        var lines = af.QuerySelectorAll(".category-line");
        Assert.That(lines[0].QuerySelector(".category-line-name")!.TextContent,
            Does.StartWith("puls-cook (claude-haiku-5-5)"));
        Assert.That(lines[0].QuerySelectorAll(".cat-bar-value").Select(v => v.TextContent),
            Is.EqualTo(new[] { "75%", "50%" }), "the latest run, r3");
        Assert.That(lines[1].QuerySelectorAll(".cat-bar-value").Select(v => v.TextContent),
            Is.EqualTo(new[] { "10%", "10%" }));
    }

    [Test]
    public void ACategoryWithTwoRunsOnALine_DrawsTheTrend_WithOnlyThePassRatePanels()
    {
        var cut = Render(Trend);

        var af = cut.FindAll(".category-card")[0];
        var panels = af.QuerySelectorAll(".trend-panel");
        Assert.That(panels, Has.Count.EqualTo(2), "pass@k and pass^k, none of the nine others");
        Assert.That(panels.Select(p => p.QuerySelector(".chart-head")!.TextContent),
            Has.All.Matches<string>(t => t.Contains("pass@k") || t.Contains("pass^k")));
        Assert.That(af.QuerySelectorAll("line.trend-point"), Has.Count.GreaterThanOrEqualTo(3),
            "r1 and r3 on the cook line, r2 on the tools line; r2 is a gap on the cook line, never a 0");
        Assert.That(af.QuerySelectorAll("path.trend-line"), Has.Count.GreaterThanOrEqualTo(1),
            "only the cook line has two points to join");
    }

    [Test]
    public void ACategoryWithOneRun_SaysThereIsNoTrendYet_InsteadOfALoneDot()
    {
        var overall = Render(Trend).FindAll(".category-card")[1];

        Assert.That(overall.QuerySelector(".category-no-trend")!.TextContent, Does.Contain("One run so far"));
        Assert.That(overall.QuerySelectorAll(".trend-panel"), Is.Empty);
        Assert.That(overall.QuerySelectorAll(".cat-bar-value").Select(v => v.TextContent),
            Is.EqualTo(new[] { "50%", "25%" }), "the numbers are still there to read");
    }

    [Test]
    public void NoCategoryWithARate_SaysSo_AndDrawsNothing()
    {
        var cut = Render("""{"check":"puls-cook","modes":[],"categories":[]}""");

        Assert.That(cut.FindAll(".category-card"), Is.Empty);
        Assert.That(cut.Find(".category-trend-none").TextContent, Does.Contain("No category has five counted trials"));
    }

    [Test]
    public void ATrendFromBeforeCategories_DrawsNothing_AndSaysTheSame()
    {
        var cut = Render("""{"check":"puls-cook","modes":[]}""");

        Assert.That(cut.FindAll(".category-card"), Is.Empty);
        Assert.That(cut.Find(".category-trend-none"), Is.Not.Null);
    }
}
