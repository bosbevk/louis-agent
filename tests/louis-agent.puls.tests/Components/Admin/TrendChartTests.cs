using System.Text.Json;
using LouisAgent.Puls.Components.Admin;

namespace LouisAgent.Puls.Tests.Components.Admin;

// F18 step 5a.2: the trend chart draws pass@k and pass^k as two more panels, a line per mode, from the points' own numbers (the API worked them out); a point
// with none leaves a gap. Written first: the chart had its nine panels and no rates.
public class TrendChartTests
{
    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    private const string Trend = """
        {"check":"puls-cook","modes":[{"name":"puls-cook","points":[
          {"run":"run_1","at":"2026-10-24T09:00:00+00:00","cost":0.1,"score":1,"conversations":8,"tokens":1000,"seconds_per_turn":9.5,"cache_share":0.5,"sourced":0.8,"tools_failed":0,"cost_per_answer":0.02,"sites":3,"pass_at_k":1,"pass_hat_k":0.5,"k":2},
          {"run":"run_2","at":"2026-10-25T09:00:00+00:00","cost":0.1,"score":1,"conversations":8,"tokens":1000,"seconds_per_turn":9.5,"cache_share":0.5,"sourced":0.8,"tools_failed":0,"cost_per_answer":0.02,"sites":3,"pass_at_k":1,"pass_hat_k":1,"k":2}]}]}
        """;

    private static IRenderedComponent<TrendChart> Render(string trend)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        return ctx.Render<TrendChart>(p => p.Add(x => x.Trend, Json(trend)));
    }

    [Test]
    public void ItDrawsPassAtKAndPassHatK_AsTwoPanelsWithALineEach()
    {
        var cut = Render(Trend);

        var panels = cut.FindAll(".trend-panel");
        Assert.That(panels, Has.Count.EqualTo(11), "the nine rings and the two rates");
        var titles = cut.FindAll(".trend-panel .chart-head").Select(h => h.TextContent).ToList();
        Assert.That(titles.Any(t => t.Contains("pass@k")), Is.True, "a panel for pass@k");
        Assert.That(titles.Any(t => t.Contains("pass^k")), Is.True, "a panel for pass^k");
        Assert.That(cut.FindAll("svg[aria-label*='pass@k'] .trend-line"), Has.Count.EqualTo(1));
        Assert.That(cut.FindAll("svg[aria-label*='pass^k'] .trend-line"), Has.Count.EqualTo(1));
    }

    [Test]
    public void ARunWithNoRates_SaysNoKnownValues_NotAZeroLine()
    {
        var cut = Render(Trend.Replace("\"pass_at_k\":1,\"pass_hat_k\":0.5,\"k\":2", "\"pass_at_k\":null,\"pass_hat_k\":null,\"k\":null")
            .Replace("\"pass_at_k\":1,\"pass_hat_k\":1,\"k\":2", "\"pass_at_k\":null,\"pass_hat_k\":null,\"k\":null"));

        var panel = cut.FindAll(".trend-panel").Single(p => p.QuerySelector(".chart-head")!.TextContent.TrimStart().StartsWith("pass@k"));
        Assert.That(panel.QuerySelector(".trend-top")!.TextContent, Is.EqualTo("no known values yet"));
        Assert.That(panel.QuerySelectorAll(".trend-line"), Is.Empty);
    }
}
