using LouisAgent.Api.Contracts.Admin.Screens;

namespace LouisAgent.Packs.Recipes.Web;

/// <summary>
/// The one line the library's view adds: of everything we offered, how much was served. It reads the screen's own rows
/// and adds no number of its own.
/// </summary>
public static class RecipeUsageLine
{
    /// <summary>The screen's name, which the pack gave it; this project can't reference the pack.</summary>
    public const string ScreenName = "recipe-library";

    /// <summary>The id of the row adding up every day.</summary>
    public const string AllDaysRow = "all";

    /// <summary>The heading of the column holding what we offered.</summary>
    public const string Offered = "Offered";

    /// <summary>The heading of the column holding what we served.</summary>
    public const string Served = "Served";

    /// <summary>The line for this page, or null when it has nothing to add up.</summary>
    public static string? Of(ScreenReply screen, ScreenPageReply page)
    {
        var offeredAt = ColumnOf(screen, Offered);
        var servedAt = ColumnOf(screen, Served);
        if (offeredAt < 0 || servedAt < 0)
            return null;

        if (page.Rows.FirstOrDefault(row => row.Id == AllDaysRow) is not { } allDays)
            return null;

        return $"Of {allDays.Cells[offeredAt]} offered, {allDays.Cells[servedAt]} served";
    }

    /// <summary>Where a heading sits among the screen's columns.</summary>
    private static int ColumnOf(ScreenReply screen, string title) =>
        screen.Columns.ToList().FindIndex(column => column.Title == title);
}
