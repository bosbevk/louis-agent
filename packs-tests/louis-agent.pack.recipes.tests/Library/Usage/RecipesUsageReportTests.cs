using LouisAgent.Composition.Packs.Reports;
using LouisAgent.Packs.Recipes.Library.Usage;

namespace LouisAgent.Packs.Recipes.Tests.Library.Usage;

/// <summary>
/// The library's counter as a table for the admin page: a line adding up every day, then the days, newest first. A
/// store that's down is an empty table, because a report never throws.
/// </summary>
public class RecipesUsageReportTests
{
    /// <summary>The report's table over a counter holding these days.</summary>
    private static Task<ReportTable> Read(params RecipeUsageDay[] days) =>
        new RecipesUsageReport(new FakeRecipeUsage(false, days)).ReadAsync(CancellationToken.None);

    /// <summary>A day of these numbers, some days into November.</summary>
    private static RecipeUsageDay Day(int day, long kept = 0, long offered = 0, long served = 0, long failed = 0) =>
        new(new DateOnly(2026, 11, day), kept, offered, served, failed);

    /// <summary>It names itself and says what its numbers are, days being UTC.</summary>
    [Test]
    public void ItSaysWhatItIs()
    {
        var report = new RecipesUsageReport(new FakeRecipeUsage());

        Assert.That(report.Name, Is.EqualTo("recipe-library"));
        Assert.That(report.Description, Does.Contain("UTC"));
    }

    /// <summary>The columns are the four numbers, in the order a recipe lives them.</summary>
    [Test]
    public async Task TheColumns_AreTheFourNumbers() =>
        Assert.That((await Read()).Columns, Is.EqualTo(new[] { "Kept", "Offered", "Served", "Failed" }));

    /// <summary>A line adds up every day, then each day follows as the counter gave it, newest first.</summary>
    [Test]
    public async Task AnAllDaysRow_ComesFirst_ThenTheDays()
    {
        var table = await Read(Day(6, 1, 2, 3, 4), Day(5, 10, 20, 30, 40));

        Assert.That(table.Rows.Select(row => row.Label), Is.EqualTo(new[] { "All days", "2026-11-06", "2026-11-05" }));
        Assert.That(table.Rows[0].Counts, Is.EqualTo(new long[] { 11, 22, 33, 44 }));
        Assert.That(table.Rows[1].Counts, Is.EqualTo(new long[] { 1, 2, 3, 4 }));
    }

    /// <summary>At most 30 days show, and the line above them still adds up all the days kept.</summary>
    [Test]
    public async Task AtMost30Days_Show_ButAllCount()
    {
        var days = Enumerable.Range(0, 40)
            .Select(back => new RecipeUsageDay(new DateOnly(2026, 11, 6).AddDays(-back), 1, 0, 0, 0)).ToArray();

        var table = await Read(days);

        Assert.That(table.Rows, Has.Count.EqualTo(31), "the all-days row and 30 days");
        Assert.That(table.Rows[0].Counts[0], Is.EqualTo(40));
        Assert.That(table.Rows[1].Label, Is.EqualTo("2026-11-06"));
    }

    /// <summary>With nothing counted yet there are no rows at all, not a line of noughts.</summary>
    [Test]
    public async Task WithNothingCounted_ThereAreNoRows() => Assert.That((await Read()).Rows, Is.Empty);

    /// <summary>A store that's down gives an empty table with its columns: a report never throws.</summary>
    [Test]
    public async Task AStoreThatsDown_IsAnEmptyTable()
    {
        var table = await new RecipesUsageReport(new FakeRecipeUsage(fails: true)).ReadAsync(CancellationToken.None);

        Assert.That(table.Rows, Is.Empty);
        Assert.That(table.Columns, Has.Count.EqualTo(4));
    }
}
