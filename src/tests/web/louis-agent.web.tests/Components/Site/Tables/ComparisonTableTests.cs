using LouisAgent.Web.Components.Site.Tables;

namespace LouisAgent.Web.Tests.Components.Site.Tables;

/// <summary>
/// F161.1: a page sets things side by side as a comparison, its topics down the side and what is compared across the
/// top, each row's cells under their columns.
/// </summary>
public class ComparisonTableTests
{
    /// <summary>Three things compared.</summary>
    private static readonly string[] Columns = ["louis-agent", "Agent Framework", "Semantic Kernel"];

    /// <summary>A row's cells line up under the columns, its topic leading the row.</summary>
    [Test]
    public void ARowsCells_LineUpUnderTheColumns()
    {
        using var ctx = new BunitContext();
        ComparisonRow[] rows = [new("Language", ["C#", "C#, Python", "C#, Python, Java"])];

        var cut = ctx.Render<ComparisonTable>(table => table
            .Add(t => t.Columns, Columns)
            .Add(t => t.Rows, rows));

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll("table.comparison thead th").Select(th => th.TextContent.Trim()),
                Is.EqualTo(new[] { "", "louis-agent", "Agent Framework", "Semantic Kernel" }));
            Assert.That(cut.Find("tbody tr th[scope='row']").TextContent.Trim(), Is.EqualTo("Language"));
            Assert.That(cut.FindAll("tbody tr td").Select(td => td.TextContent.Trim()),
                Is.EqualTo(new[] { "C#", "C#, Python", "C#, Python, Java" }));
        });
    }

    /// <summary>A row with too few cells is refused, with which row and how many it needs.</summary>
    [Test]
    public void ARowWithTooFewCells_IsRefused()
    {
        using var ctx = new BunitContext();
        ComparisonRow[] rows = [new("Hosting", ["Your own hardware"])];

        var refused = Assert.Throws<InvalidOperationException>(() => ctx.Render<ComparisonTable>(table => table
            .Add(t => t.Columns, Columns)
            .Add(t => t.Rows, rows)));

        Assert.That(refused!.Message, Does.Contain("Hosting").And.Contain("3"));
    }
}
