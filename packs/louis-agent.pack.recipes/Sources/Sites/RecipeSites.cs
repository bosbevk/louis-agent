using System.Globalization;
using LouisAgent.Choices;
using LouisAgent.Core.Messages;
using LouisAgent.Packs.Recipes.Messages;

namespace LouisAgent.Packs.Recipes.Sources.Sites;

/// <summary>
/// Every recipe site we may read, one row each, read from the table in <c>recipe-sites.md</c>. Ask for the list, or
/// for one site by its slug.
/// </summary>
/// <remarks>
/// A Registry loaded from content (Fowler, PoEAA). The sites are rows, not an enum, which is a stated exception to
/// ADR 0043: a site is data, not a code path, so a site that turns <c>noai</c> is a change to the table and not a
/// release. What that costs: a slug is a string checked at run time, and the table's own tests are what catch a typo.
/// The table is read with a plain walk over its lines and a split on <c>|</c>, never a regex (ADR 0057), and a row
/// that does not fit throws with its number, so a broken site is never skipped quietly.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0043-repeated-names-are-enums.md">ADR 0043: repeated names are enums</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0011-prompts-are-content.md">ADR 0011: prompts are content</seealso>
public static class RecipeSites
{
    private const int Columns = 7;
    private const string Resource = "recipe-sites.md";
    private static readonly string[] ColumnNames = ["slug", "name", "home", "read by", "search", "checked", "signals"];

    /// <summary>The table, read once.</summary>
    public static IReadOnlyList<RecipeSite> All { get; } = Parse(ReadTable());

    private static readonly Dictionary<string, RecipeSite> BySlug =
        All.ToDictionary(site => site.Slug, StringComparer.OrdinalIgnoreCase);

    /// <summary>The site with this slug, whatever its case, or null when there is none.</summary>
    public static RecipeSite? Find(string slug) => BySlug.GetValueOrDefault(slug.Trim());

    /// <summary>
    /// Turns the table's text into rows. The first two table lines are the header and its rule; every later line
    /// that starts with <c>|</c> is a site, numbered from 1.
    /// </summary>
    internal static IReadOnlyList<RecipeSite> Parse(string table) =>
    [
        .. table.Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.StartsWith('|'))
            .Skip(2)
            .Select((line, index) => ParseRow(index + 1, line))
    ];

    /// <summary>One line of the table as a site, or a <see cref="FormatException"/> naming the wrong row.</summary>
    private static RecipeSite ParseRow(int row, string line)
    {
        // Cut off the outer bars, then split on the rest: seven cells, the last one allowed to be empty.
        string[] cells = [.. line.Trim('|').Split('|').Select(cell => cell.Trim())];
        if (cells.Length != Columns)
            throw new FormatException(MessageText.Format(RecipeErrorMessage.SiteRowColumns,
                new
                {
                    Row = row,
                    Count = cells.Length,
                    Expected = Columns
                }));

        return new RecipeSite(
            cells[0],
            cells[1],
            Home(row, cells[2]),
            Choice<RecipeReadBy>(row, 3, cells[3]),
            Choice<RecipeSearch>(row, 4, cells[4]),
            Checked(row, cells[5]),
            cells[6].Length == 0 ? null : cells[6]);
    }

    /// <summary>The home as an absolute https address, or the row's error.</summary>
    private static Uri Home(int row, string cell) =>
        Uri.TryCreate(cell, UriKind.Absolute, out Uri? home) && home.Scheme == Uri.UriSchemeHttps
            ? home
            : throw Bad(row, 2, cell);

    /// <summary>The cell as one of the enum's members, parsed once with the repo's parser (ADR 0024).</summary>
    private static T Choice<T>(int row, int column, string cell) where T : struct, Enum =>
        SettingChoice.TryParse(cell, out T? choice) && choice is { } member ? member : throw Bad(row, column, cell);

    /// <summary>The day the site was checked, written as 2026-11-04.</summary>
    private static DateOnly Checked(int row, string cell) =>
        DateOnly.TryParseExact(cell, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day)
            ? day
            : throw Bad(row, 5, cell);

    /// <summary>The error for a cell that is wrong, naming its row and column.</summary>
    private static FormatException Bad(int row, int column, string value) =>
        new(MessageText.Format(RecipeErrorMessage.SiteRowValue,
            new
            {
                Row = row,
                Value = value,
                Column = ColumnNames[column]
            }));

    /// <summary>The embedded table's text; it is part of the pack, so a missing one is a broken build.</summary>
    private static string ReadTable()
    {
        string name = $"{typeof(RecipeSites).Namespace}.{Resource}";
        using var stream = typeof(RecipeSites).Assembly.GetManifestResourceStream(name)
                           ?? throw new InvalidOperationException(MessageText.Format(RecipeErrorMessage.SitesMissing));
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
