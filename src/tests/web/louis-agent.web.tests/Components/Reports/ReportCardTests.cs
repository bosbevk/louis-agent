using LouisAgent.Web.Components.Reports;
using LouisAgent.Api.Contracts.Admin.Reports;

namespace LouisAgent.Web.Tests.Components.Reports;

/// <summary>
/// F35.7: a report's card shows the table the API gave it, knowing nothing about which pack it came from, pinned to
/// ReportCard.html. With no rows it says nothing's been counted yet.
/// </summary>
public class ReportCardTests
{
    /// <summary>A report with an all-days row and one day.</summary>
    private static readonly ReportReply Counted = new("usage", "What was used.", ["Kept", "Served"],
    [
        new ReportRowReply("All days", [11, 22]),
        new ReportRowReply("2026-11-06", [1, 2])
    ]);

    /// <summary>A report's name, description, columns and rows are all there, in order.</summary>
    [Test]
    public void AReport_MatchesTheGoldenFile()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<ReportCard>(p => p.Add(c => c.Report, Counted));

        Assert.That(cut.FindAll(".report-row").Select(row => row.QuerySelector("td")!.TextContent),
            Is.EqualTo(new[] { "All days", "2026-11-06" }));
        Snapshots.Match("Components/Reports/ReportCard.html", cut.Markup);
    }

    /// <summary>With no rows there's a line saying so, and no empty table.</summary>
    [Test]
    public void AReportWithNoRows_SaysSo()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<ReportCard>(p => p.Add(c => c.Report, Counted with { Rows = [] }));

        Assert.That(cut.Markup, Does.Contain("Nothing counted yet"));
        Assert.That(cut.FindAll("table"), Is.Empty);
    }
}
