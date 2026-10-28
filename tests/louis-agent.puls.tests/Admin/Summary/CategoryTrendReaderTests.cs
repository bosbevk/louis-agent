using System.Text.Json;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Trends;
using LouisAgent.Puls.Admin.Summary;

namespace LouisAgent.Puls.Tests.Admin.Summary;

/// <summary>
/// The trend-by-category rework (Louis, 2026-10-26: "I need to know what it tells me"): each category is a card that
/// says, per mode, the latest run's pass@k and pass^k as numbers, how many runs stand behind it, and whether there is
/// a trend to draw yet (two runs or more on some line). The categories are the contract's rows.
/// </summary>
public class CategoryTrendReaderTests
{
    /// <summary>A category's row, read from its JSON as the API sends it.</summary>
    private static CategoryTrendRow Row(string text) =>
        JsonSerializer.Deserialize<CategoryTrendRow>(text, AdminJson.SnakeCase)!;

    /// <summary>A category with two lines: one over two runs, one over a single run.</summary>
    private static readonly string Category = SampleMode.Fill(
        """
        {"category":"budget","modes":[
          {"name":"<ModeBeta> (claude-haiku-5-5)","model":"claude-haiku-5-5","points":[
            {"run":"r1","at":"2026-10-24T09:00:00+00:00","pass_rate":0.5,"pass_at_k":0.5,"pass_hat_k":0.25,"n":6},
            {"run":"r3","at":"2026-10-26T09:00:00+00:00","pass_rate":0.67,"pass_at_k":1.0,"pass_hat_k":0.5,"n":6}]},
          {"name":"<ModeGamma>","model":null,"points":[
            {"run":"r3","at":"2026-10-26T09:00:00+00:00","pass_rate":0,"pass_at_k":0,"pass_hat_k":0,"n":6}]}]}
        """);

    /// <summary>Each line has its latest point and its run count.</summary>
    [Test]
    public void EachLine_HasItsLatestPoint_AndItsRunCount()
    {
        var card = CategoryTrendReader.Read(Row(Category));

        Assert.That(card.Name, Is.EqualTo("budget"));
        Assert.That(card.Lines.Select(l => (l.Name, l.Runs, l.PassAtK, l.PassHatK, l.N)),
            Is.EqualTo(new[]
            {
                ($"{SampleMode.Beta} (claude-haiku-5-5)", 2, (double?)1.0, (double?)0.5, 6),
                (SampleMode.Gamma, 1, (double?)0.0, (double?)0.0, 6),
            }));
    }

    /// <summary>The latest is by time, not by order.</summary>
    [Test]
    public void TheLatestIsByTime_NotByOrder()
    {
        var card = CategoryTrendReader.Read(Row(
            """
            {"category":"x","modes":[{"name":"m","model":null,"points":[
              {"run":"new","at":"2026-10-26T09:00:00+00:00","pass_rate":1,"pass_at_k":1,"pass_hat_k":1,"n":6},
              {"run":"old","at":"2026-10-20T09:00:00+00:00","pass_rate":0,"pass_at_k":0,"pass_hat_k":0,"n":6}]}]}
            """));

        Assert.That(card.Lines.Single().LatestRun, Is.EqualTo("new"));
    }

    /// <summary>There is a trend to draw only with two runs or more on some line.</summary>
    [Test]
    public void ThereIsATrendToDraw_OnlyWithTwoRunsOrMoreOnSomeLine()
    {
        Assert.That(CategoryTrendReader.Read(Row(Category)).HasTrend, Is.True);
        var one = Row(
            """
            {"category":"x","modes":[{"name":"m","model":null,"points":[
              {"run":"r","at":"2026-10-26T09:00:00+00:00","pass_rate":1,"pass_at_k":1,"pass_hat_k":1,"n":6}]}]}
            """);
        Assert.That(CategoryTrendReader.Read(one).HasTrend, Is.False);
    }

    /// <summary>The card's run count is the distinct runs across its lines.</summary>
    [Test]
    public void TheRunCountOfTheCard_IsTheDistinctRunsAcrossItsLines() =>
        Assert.That(CategoryTrendReader.Read(Row(Category)).Runs, Is.EqualTo(2));

    /// <summary>The card's chart is its lines with their pass rates, in the trend chart's shape.</summary>
    [Test]
    public void TheChart_IsTheLinesPassRates_InTheTrendChartsShape()
    {
        var chart = CategoryTrendReader.Read(Row(Category)).Chart;

        Assert.That(chart.Select(l => l.Name),
            Is.EqualTo(new[] { $"{SampleMode.Beta} (claude-haiku-5-5)", SampleMode.Gamma }));
        Assert.That(chart[0].Points.Select(p => (p.Run, p.PassAtK, p.PassHatK)),
            Is.EqualTo(new[] { ("r1", (double?)0.5, (double?)0.25), ("r3", (double?)1.0, (double?)0.5) }));
    }
}
