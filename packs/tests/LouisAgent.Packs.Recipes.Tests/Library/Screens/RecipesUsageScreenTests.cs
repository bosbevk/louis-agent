using LouisAgent.Composition.Packs.Screens;
using LouisAgent.Composition.Packs.Screens.Reading;
using LouisAgent.Packs.Recipes.Library.Screens;
using LouisAgent.Packs.Recipes.Library.Usage;
using LouisAgent.Packs.Recipes.Tests.Library.Usage;

namespace LouisAgent.Packs.Recipes.Tests.Library.Screens;

/// <summary>
/// The library's counter as a screen: a line adding up every day, then the days, newest first. A store that's down is
/// an empty page, because a screen's read never throws.
/// </summary>
public class RecipesUsageScreenTests
{
    /// <summary>The screen's first page over a counter holding these days.</summary>
    private static Task<ScreenPage> Read(params RecipeUsageDay[] days) =>
        new RecipesUsageScreen(new FakeRecipeUsage(false, days)).ReadAsync(new ScreenQuery(null, 1), CancellationToken.None);

    /// <summary>A day of these numbers, some days into November.</summary>
    private static RecipeUsageDay Day(int day, long kept = 0, long offered = 0, long served = 0, long failed = 0) =>
        new(new DateOnly(2026, 11, day), kept, offered, served, failed);

    /// <summary>It names itself and says what its numbers are, days being UTC.</summary>
    [Test]
    public void ItSaysWhatItIs()
    {
        var spec = new RecipesUsageScreen(new FakeRecipeUsage()).Spec;

        Assert.That(spec.Name, Is.EqualTo("recipe-library"));
        Assert.That(spec.Title, Is.EqualTo("Recipe library"));
        Assert.That(spec.Description, Does.Contain("UTC"));
        Assert.That(spec.Searchable, Is.False);
    }

    /// <summary>The columns are the day, then the four numbers in the order a recipe lives them.</summary>
    [Test]
    public void TheColumns_AreTheDay_ThenTheFourNumbers()
    {
        var columns = new RecipesUsageScreen(new FakeRecipeUsage()).Spec.Columns;

        Assert.That(columns.Select(column => column.Title), Is.EqualTo(new[] { "Day", "Kept", "Offered", "Served", "Failed" }));
        Assert.That(columns[0].Kind, Is.EqualTo(ScreenCell.Text));
        Assert.That(columns.Skip(1).Select(column => column.Kind), Is.All.EqualTo(ScreenCell.Number));
    }

    /// <summary>A line adds up every day, then each day follows as the counter gave it, newest first.</summary>
    [Test]
    public async Task AnAllDaysRow_ComesFirst_ThenTheDays()
    {
        var page = await Read(Day(6, 1, 2, 3, 4), Day(5, 10, 20, 30, 40));

        Assert.That(page.Rows.Select(row => row.Cells[0]), Is.EqualTo(new[] { "All days", "2026-11-06", "2026-11-05" }));
        Assert.That(page.Rows[0].Cells.Skip(1), Is.EqualTo(new[] { "11", "22", "33", "44" }));
        Assert.That(page.Rows[1].Cells.Skip(1), Is.EqualTo(new[] { "1", "2", "3", "4" }));
        Assert.That(page.Total, Is.EqualTo(3));
    }

    /// <summary>Each row has an id of its own, as a screen's rows must.</summary>
    [Test]
    public async Task EachRow_HasItsOwnId()
    {
        var page = await Read(Day(6), Day(5));

        Assert.That(page.Rows.Select(row => row.Id), Is.Unique);
    }

    /// <summary>At most 30 days show, and the line above them still adds up all the days kept.</summary>
    [Test]
    public async Task AtMost30Days_Show_ButAllCount()
    {
        var days = Enumerable.Range(0, 40)
            .Select(back => new RecipeUsageDay(new DateOnly(2026, 11, 6).AddDays(-back), 1, 0, 0, 0)).ToArray();

        var page = await Read(days);

        Assert.That(page.Rows, Has.Count.EqualTo(31), "the all-days row and 30 days");
        Assert.That(page.Rows[0].Cells[1], Is.EqualTo("40"));
        Assert.That(page.Rows[1].Cells[0], Is.EqualTo("2026-11-06"));
    }

    /// <summary>It all fits on the first page, so a later page has no rows, with the same total.</summary>
    [Test]
    public async Task ALaterPage_HasNoRows()
    {
        var screen = new RecipesUsageScreen(new FakeRecipeUsage(false, Day(6)));

        var page = await screen.ReadAsync(new ScreenQuery(null, 2), CancellationToken.None);

        Assert.That(page.Rows, Is.Empty);
        Assert.That(page.Total, Is.EqualTo(2));
    }

    /// <summary>With nothing counted yet there are no rows at all, not a line of noughts.</summary>
    [Test]
    public async Task WithNothingCounted_ThereAreNoRows() => Assert.That((await Read()).Rows, Is.Empty);

    /// <summary>A store that's down gives an empty page: a screen's read never throws.</summary>
    [Test]
    public async Task AStoreThatsDown_IsAnEmptyPage()
    {
        var screen = new RecipesUsageScreen(new FakeRecipeUsage(fails: true));

        var page = await screen.ReadAsync(new ScreenQuery(null, 1), CancellationToken.None);

        Assert.That(page, Is.EqualTo(ScreenPage.Empty));
    }
}
