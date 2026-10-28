using System.Text.Json;
using System.Text.RegularExpressions;
using LouisAgent.Puls.Components.Admin.Trends;
using LouisAgent.Web.Testing;

namespace LouisAgent.Puls.Tests.Components.Admin.Trends;

/// <summary>
/// Pins what the trend chart draws, the full chart and the card's small one, so the rebuild of the chart keeps every
/// panel, line, point and label (goldens beside this test).
/// </summary>
public partial class TrendChartSnapshotTests
{
    private const string Trend = """
                                 {"check":"puls-cook","categories":[],"modes":[
                                  {"name":"puls-cook","points":[
                                   {"run":"run_1","at":"2026-10-24T09:00:00+00:00","cost":0.1,"score":1,"conversations":8,"tokens":1000,"seconds_per_turn":9.5,"cache_share":0.5,"sourced":0.8,"tools_failed":0,"cost_per_answer":0.02,"sites":3,"pass_at_k":1,"pass_hat_k":0.5,"k":2},
                                   {"run":"run_2","at":"2026-10-25T09:00:00+00:00","cost":0.2,"score":0.5,"conversations":8,"tokens":2000,"seconds_per_turn":12,"cache_share":0.7,"sourced":0.6,"tools_failed":0.1,"cost_per_answer":0.05,"sites":5,"pass_at_k":1,"pass_hat_k":1,"k":2}]},
                                  {"name":"puls","points":[
                                   {"run":"run_2","at":"2026-10-25T09:00:00+00:00","cost":0.05,"score":1,"conversations":8,"tokens":500,"seconds_per_turn":4,"cache_share":0.9,"sourced":null,"tools_failed":0,"cost_per_answer":null,"sites":0,"pass_at_k":0.5,"pass_hat_k":0.5,"k":2}]}]}
                                 """;

    /// <summary>The full chart: every panel, a line per mode, the reading run, the dates.</summary>
    [Test]
    public void TheFullChart_IsDrawnAsBefore()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<TrendChart>(p => p
            .Add(x => x.Trend, Json(Trend))
            .Add(x => x.Highlight, "run_1"));

        Snapshots.Match("louis-agent.puls.tests", "Components/Admin/Trends/TrendChart.full.html", Stable(cut.Markup));
    }

    /// <summary>The card's small chart: the cost panel alone.</summary>
    [Test]
    public void TheSmallChart_IsDrawnAsBefore()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<TrendChart>(p => p
            .Add(x => x.Trend, Json(Trend))
            .Add(x => x.Small, true));

        Snapshots.Match("louis-agent.puls.tests", "Components/Admin/Trends/TrendChart.small.html", Stable(cut.Markup));
    }

    /// <summary>
    /// A line whose second run has a note and a changed prompt: every panel marks that run once of each kind, with
    /// its tooltip, and the legend explains both marks.
    /// </summary>
    [Test]
    public void ANoteAndAPromptChange_AreMarkedInEveryPanel()
    {
        using var ctx = new BunitContext();
        string marked = Trend.Replace("\"pass_hat_k\":1,\"k\":2}]}",
            "\"pass_hat_k\":1,\"k\":2,\"notes\":[\"judge changed\"],\"prompt_changed\":[\"sub-session:puls-cook\"]}]}");

        var cut = ctx.Render<TrendChart>(p => p.Add(x => x.Trend, Json(marked)));

        Assert.That(cut.FindAll(".trend-panel .trend-note"), Has.Count.EqualTo(11), "one per panel");
        Assert.That(cut.FindAll(".trend-panel .trend-prompt"), Has.Count.EqualTo(11), "one per panel");
        Snapshots.Match("louis-agent.puls.tests", "Components/Admin/Trends/TrendChart.marks.html", Stable(cut.Markup));
    }

    /// <summary>The card's small chart is the cost at a glance: it draws no marks.</summary>
    [Test]
    public void TheSmallChart_DrawsNoMarks()
    {
        using var ctx = new BunitContext();
        string marked = Trend.Replace("\"pass_hat_k\":1,\"k\":2}]}",
            "\"pass_hat_k\":1,\"k\":2,\"notes\":[\"judge changed\"],\"prompt_changed\":[\"sub-session:puls-cook\"]}]}");

        var cut = ctx.Render<TrendChart>(p => p
            .Add(x => x.Trend, Json(marked))
            .Add(x => x.Small, true));

        Assert.That(cut.FindAll(".trend-note, .trend-prompt"), Is.Empty);
    }

    /// <summary>
    /// The markup made the same on any machine: each chart title's note id (fresh on every render), and a run's time
    /// of day, which the chart writes in the reader's own time zone (CI runs in UTC).
    /// </summary>
    private static string Stable(string markup) =>
        LocalTime().Replace(TipId().Replace(markup, "tip-ID"), "$1 HH:mm");

    /// <summary>A date and a time of day as the chart writes them, "2026-10-24 11:00".</summary>
    [GeneratedRegex(@"(\d{4}-\d{2}-\d{2}) \d{2}:\d{2}")]
    private static partial Regex LocalTime();

    /// <summary>A note's id: "tip-" and eight hex digits.</summary>
    [GeneratedRegex("tip-[0-9a-f]{8}")]
    private static partial Regex TipId();

    /// <summary>The trend as the API sends it.</summary>
    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();
}
