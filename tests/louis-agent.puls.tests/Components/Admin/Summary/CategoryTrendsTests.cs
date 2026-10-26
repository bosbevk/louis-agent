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

    [Test]
    public void EachCategory_HasItsOwnChart_NamedForIt_WithOnlyThePassRatePanels()
    {
        var cut = Render(Trend);

        var charts = cut.FindAll(".category-trend");
        Assert.That(charts.Select(c => c.QuerySelector("h4")!.TextContent), Is.EqualTo(new[] { "language:af", "overall" }));
        Assert.That(charts[0].QuerySelectorAll(".trend-panel").Select(p => p.QuerySelector(".chart-head")!.TextContent), Has.All.Matches<string>(t => t.Contains("pass@k") || t.Contains("pass^k")));
        Assert.That(charts[0].QuerySelectorAll(".trend-panel"), Has.Count.EqualTo(2), "pass@k and pass^k, none of the nine others");
    }

    [Test]
    public void ARunWithNoPointForALine_IsAGapInIt_NotAZero()
    {
        var cut = Render(Trend);

        // language:af: the cook line has r1 and r3 (r2 had too few trials for it: no point), the tools line has r2 only; the x axis is every run, so each line has a gap.
        var passAt = cut.FindAll(".category-trend")[0].QuerySelectorAll(".trend-panel")[0];
        Assert.That(passAt.QuerySelectorAll("line.trend-point"), Has.Count.EqualTo(3), "2 points on the cook line, 1 on the tools line, none at 0");
        Assert.That(passAt.QuerySelectorAll("path.trend-line"), Has.Count.EqualTo(1), "only the cook line has two points to join");
    }

    [Test]
    public void NoCategoryWithARate_SaysSo_AndDrawsNothing()
    {
        var cut = Render("""{"check":"puls-cook","modes":[],"categories":[]}""");

        Assert.That(cut.FindAll(".category-trend"), Is.Empty);
        Assert.That(cut.Find(".category-trend-none").TextContent, Does.Contain("No category has five counted trials"));
    }

    [Test]
    public void ATrendFromBeforeCategories_DrawsNothing_AndSaysTheSame()
    {
        var cut = Render("""{"check":"puls-cook","modes":[]}""");

        Assert.That(cut.FindAll(".category-trend"), Is.Empty);
        Assert.That(cut.Find(".category-trend-none"), Is.Not.Null);
    }
}
