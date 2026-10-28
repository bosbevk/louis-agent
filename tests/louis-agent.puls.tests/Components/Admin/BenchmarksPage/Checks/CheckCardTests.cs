using LouisAgent.Api.Contracts.Admin.Checks;
using LouisAgent.Api.Contracts.Admin.Trends;
using LouisAgent.Puls.Components.Admin.BenchmarksPage.Checks;

namespace LouisAgent.Puls.Tests.Components.Admin.BenchmarksPage.Checks;

/// <summary>
/// A check's card draws its trend's cost, read from the contract's trend reply; a check with no trend draws no chart.
/// </summary>
public class CheckCardTests
{
    /// <summary>The check the card is about.</summary>
    private static readonly CheckRow Check = new(SampleEval.Beta, "Puls cooks.", false);

    /// <summary>One mode over two priced runs.</summary>
    private static readonly TrendReply Trend = new(SampleEval.Beta, [],
    [
        new TrendLineRow(SampleMode.Alpha,
        [
            Point("run1", new DateTimeOffset(2026, 10, 25, 9, 0, 0, TimeSpan.Zero), 0.1m),
            Point("run2", new DateTimeOffset(2026, 10, 26, 9, 0, 0, TimeSpan.Zero), 0.5m),
        ]),
    ]);

    /// <summary>A trend point with only its run, time and cost.</summary>
    private static TrendPointRow Point(string run, DateTimeOffset at, decimal cost) =>
        new(run, at, cost, null, 2, null, null, null, null, null, null, null, null, null, null);

    /// <summary>The card for the check, with or without a trend.</summary>
    private static IRenderedComponent<CheckCard> Render(TrendReply? trend)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        return ctx.Render<CheckCard>(p => p.Add(x => x.Check, Check).Add(x => x.Runs, []).Add(x => x.Trend, trend)
            .Add(x => x.Open, true));
    }

    /// <summary>A trend draws the small chart: its cost panel, one line over the two runs.</summary>
    [Test]
    public void ATrend_DrawsTheSmallChart_ItsCostOverTheRuns()
    {
        var cut = Render(Trend);

        Assert.That(cut.FindAll(".trend.small .trend-panel"), Has.Count.EqualTo(1));
        Assert.That(cut.FindAll(".trend.small path.trend-line"), Has.Count.EqualTo(1));
    }

    /// <summary>No trend, no chart.</summary>
    [Test]
    public void NoTrend_DrawsNoChart() =>
        Assert.That(Render(null).FindAll(".trend"), Is.Empty);
}
