using System.Text.Json;
using LouisAgent.Puls.Components.Admin.Trends;

namespace LouisAgent.Puls.Tests.Components.Admin.Trends;

/// <summary>
/// The trend chart draws a panel per measure, a line per mode, from the numbers the API worked out. Its heads read the
/// run you're looking at, with how it moved since the run before, and hovering a point reads that run in its own panel.
/// </summary>
public class TrendChartTests
{
    /// <summary>Turns a fixture's text into the JSON the chart takes.</summary>
    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    // Two runs of one mode, both with their pass@k and pass^k.
    private const string Trend = """
                                 {"check":"puls-cook","modes":[{"name":"puls-cook","points":[
                                   {"run":"run_1","at":"2026-10-24T09:00:00+00:00","cost":0.1,"score":1,"conversations":8,"tokens":1000,"seconds_per_turn":9.5,"cache_share":0.5,"sourced":0.8,"tools_failed":0,"cost_per_answer":0.02,"sites":3,"pass_at_k":1,"pass_hat_k":0.5,"k":2},
                                   {"run":"run_2","at":"2026-10-25T09:00:00+00:00","cost":0.1,"score":1,"conversations":8,"tokens":1000,"seconds_per_turn":9.5,"cache_share":0.5,"sourced":0.8,"tools_failed":0,"cost_per_answer":0.02,"sites":3,"pass_at_k":1,"pass_hat_k":1,"k":2}]}]}
                                 """;

    /// <summary>Draws the chart for a trend, with no run marked.</summary>
    private static IRenderedComponent<TrendChart> Render(string trend)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        return ctx.Render<TrendChart>(p => p.Add(x => x.Trend, Json(trend)));
    }

    /// <summary>
    /// Next to the nine usual panels, pass@k and pass^k get a panel each, with a line per mode drawn from the points'
    /// own numbers.
    /// </summary>
    [Test]
    public void ItDrawsPassAtKAndPassHatK_AsTwoPanelsWithALineEach()
    {
        // F18 step 5a.2: written first, when the chart had its nine panels and no rates.
        var cut = Render(Trend);

        var panels = cut.FindAll(".trend-panel");
        Assert.That(panels, Has.Count.EqualTo(11), "the nine rings and the two rates");
        var titles = cut.FindAll(".trend-panel .chart-head").Select(h => h.TextContent).ToList();
        Assert.That(titles.Any(t => t.Contains("pass@k")), Is.True, "a panel for pass@k");
        Assert.That(titles.Any(t => t.Contains("pass^k")), Is.True, "a panel for pass^k");
        Assert.That(cut.FindAll("svg[aria-label*='pass@k'] .trend-line"), Has.Count.EqualTo(1));
        Assert.That(cut.FindAll("svg[aria-label*='pass^k'] .trend-line"), Has.Count.EqualTo(1));
    }

    /// <summary>A run with no rates says so instead of drawing a zero line.</summary>
    [Test]
    public void ARunWithNoRates_SaysNoKnownValues_NotAZeroLine()
    {
        var cut = Render(Trend
            .Replace("\"pass_at_k\":1,\"pass_hat_k\":0.5,\"k\":2", "\"pass_at_k\":null,\"pass_hat_k\":null,\"k\":null")
            .Replace("\"pass_at_k\":1,\"pass_hat_k\":1,\"k\":2", "\"pass_at_k\":null,\"pass_hat_k\":null,\"k\":null"));

        var panel = cut.FindAll(".trend-panel")
            .Single(p => p.QuerySelector(".chart-head")!.TextContent.TrimStart().StartsWith("pass@k"));
        Assert.That(panel.QuerySelector(".trend-top")!.TextContent, Is.EqualTo("no known values yet"));
        Assert.That(panel.QuerySelectorAll(".trend-line"), Is.Empty);
    }

    // Two runs whose cost and rates differ, so each panel has a change to show.
    private const string TwoRuns = """
                                   {"check":"puls-cook","modes":[{"name":"puls-cook","points":[
                                     {"run":"run_1","at":"2026-10-24T09:00:00+00:00","cost":0.10,"score":1,"conversations":8,"tokens":1000,"seconds_per_turn":9.5,"cache_share":0.5,"sourced":0.8,"tools_failed":0,"cost_per_answer":0.02,"sites":3,"pass_at_k":0.5,"pass_hat_k":0.5,"k":2},
                                     {"run":"run_2","at":"2026-10-25T09:00:00+00:00","cost":0.20,"score":1,"conversations":8,"tokens":1000,"seconds_per_turn":9.5,"cache_share":0.5,"sourced":0.8,"tools_failed":0,"cost_per_answer":0.02,"sites":3,"pass_at_k":0.75,"pass_hat_k":0.25,"k":2}]}]}
                                   """;

    /// <summary>
    /// Finds a panel by the start of its title. Only the title counts: an info tip may name another panel's measure.
    /// </summary>
    private static AngleSharp.Dom.IElement PanelTitled(IRenderedComponent<TrendChart> cut, string title) =>
        cut.FindAll(".trend-panel").First(p =>
            p.QuerySelector(".chart-title > strong")!.TextContent.StartsWith(title, StringComparison.Ordinal));

    /// <summary>Draws the chart for a trend with one run marked, as when you come from that run's page.</summary>
    private static IRenderedComponent<TrendChart> RenderMarked(string trend, string highlight)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        return ctx.Render<TrendChart>(p => p.Add(x => x.Trend, Json(trend)).Add(x => x.Highlight, highlight));
    }

    /// <summary>
    /// Each panel's head reads the shown run's value per mode, with how far it moved since the run before. The shown
    /// run is the one hovered, else the one marked, else the last.
    /// </summary>
    [Test]
    public void EachPanelsHead_ReadsTheMarkedRunsValue_WithItsDeltaAgainstTheRunBefore()
    {
        // Louis, 2026-10-26: "more explanatory and maybe more interactive". Written first: the head said only "top".
        var cut = RenderMarked(TwoRuns, "run_2");

        var passAt = PanelTitled(cut, "pass@k");
        Assert.That(passAt.QuerySelector(".trend-values")!.TextContent, Does.Contain("75%"));
        Assert.That(passAt.QuerySelector(".trend-delta.up")!.TextContent, Does.Contain("+25"), "run_2 against run_1");
        var passHat = PanelTitled(cut, "pass^k");
        Assert.That(passHat.QuerySelector(".trend-delta.down")!.TextContent, Does.Contain("25"));
        Assert.That(cut.Find(".trend-shown").TextContent, Does.Contain("run_2"),
            "the trend says which run it is reading");
    }

    /// <summary>The scale says where it starts and ends, and the runs' dates sit under the axis.</summary>
    [Test]
    public void TheScaleSaysItsEnds_AndTheDatesSitUnderTheAxis()
    {
        var cut = RenderMarked(TwoRuns, "run_2");

        var cost = PanelTitled(cut, "Cost");
        Assert.That(cost.QuerySelector(".trend-top")!.TextContent, Does.StartWith("0 –"));
        Assert.That(cost.QuerySelector(".trend-x")!.TextContent, Does.Contain("2026-10-24").And.Contain("2026-10-25"));
    }

    /// <summary>
    /// Hovering a point reads that run in its own panel only; the other panels keep reading the marked run.
    /// </summary>
    [Test]
    public void HoveringAPoint_ReadsThatRunInItsOwnPanelOnly()
    {
        // Louis, 2026-10-26: "can we only update the graph I'm on?"
        var cut = RenderMarked(TwoRuns, "run_2");

        PanelTitled(cut, "pass@k").QuerySelectorAll("a.trend-run").First(a => a.GetAttribute("href")!.EndsWith("run_1"))
            .MouseEnter();

        var passAt = PanelTitled(cut, "pass@k");
        Assert.That(passAt.QuerySelector(".trend-values")!.TextContent, Does.Contain("50%"),
            "run_1 in the panel hovered");
        Assert.That(passAt.QuerySelector(".trend-reading")!.TextContent, Does.Contain("run_1"),
            "the panel says which run it reads");
        Assert.That(passAt.QuerySelectorAll(".trend-hover"), Has.Count.EqualTo(1),
            "a guide line at the hovered run, in this panel");
        var passHat = PanelTitled(cut, "pass^k");
        Assert.That(passHat.QuerySelector(".trend-values")!.TextContent, Does.Contain("25%"),
            "run_2 still, the marked run");
        Assert.That(passHat.QuerySelectorAll(".trend-hover"), Is.Empty);
        Assert.That(cut.Find(".trend-shown").TextContent, Does.Contain("run_2"),
            "the trend's own line keeps the marked run");
    }
}
