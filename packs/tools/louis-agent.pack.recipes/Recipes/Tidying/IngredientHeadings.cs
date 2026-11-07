namespace LouisAgent.Packs.Recipes.Recipes.Tidying;

/// <summary>
/// The headings a page's data left among its ingredient lines, as the sections they are: <c>For the sauce:</c> or
/// <c>SYRUP</c> is nothing to buy. What a page's buttons and dates left there goes.
/// </summary>
/// <remarks>
/// Plain string checks (ADR 0057). A line with a number is an ingredient, whatever else it looks like, and a page in
/// capitals throughout has ingredients and no headings, so a line in capitals only counts when most lines aren't.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0057-regex-is-the-last-option.md">ADR 0057: a regular expression is the last option</seealso>
internal static class IngredientHeadings
{
    /// <summary>Longer than this and a line ending in a colon is a sentence, not a heading.</summary>
    private const int LongestHeading = 40;

    /// <summary>A date is at least this long: <c>1/2/20</c>.</summary>
    private const int ShortestDate = 6;

    /// <summary>The fewest letters a word in capitals needs to be a heading.</summary>
    private const int ShortestCapitals = 3;

    /// <summary>A page's buttons that came along with its text.</summary>
    private static readonly HashSet<string> Buttons =
        new(["pin dit", "pin it", "print", "druk"], StringComparer.OrdinalIgnoreCase);

    /// <summary>The lines to buy, each under the heading above it, or the page's own section when it has one.</summary>
    public static IReadOnlyList<RecipeIngredient> Of(IReadOnlyList<RecipeIngredient> lines)
    {
        bool capitalsAreHeadings = lines.Count(line => InCapitals(Words(line.Text))) * 2 <= lines.Count;

        var kept = new List<RecipeIngredient>();
        string? heading = null;
        string? under = null;
        foreach (var line in lines)
        {
            string words = Words(line.Text);
            if (IsLeftByThePage(words)) continue;

            // A page's own section starts afresh, so a heading doesn't run on past it.
            if (line.Section != under) (heading, under) = (null, line.Section);
            if (HeadingIn(words, capitalsAreHeadings) is { } found)
            {
                heading = found;
                continue;
            }

            kept.Add(heading is null ? line : line with { Section = heading });
        }

        return kept;
    }

    /// <summary>The line up to any markup the page left on it: <c>FILLING&lt;/h3.</c> is <c>FILLING</c>.</summary>
    private static string Words(string line)
    {
        int markup = line.IndexOf('<');
        return (markup >= 0 ? line[..markup] : line).Trim();
    }

    /// <summary>The heading this line is, if it is one.</summary>
    /// <remarks>Guard clauses (ADR 0051).</remarks>
    private static string? HeadingIn(string words, bool capitalsAreHeadings)
    {
        if (words.Length == 0 || words.Any(char.IsDigit)) return null;
        if (words.EndsWith(':')) return words.TrimEnd(':').Trim();
        return capitalsAreHeadings && InCapitals(words) ? words : null;
    }

    /// <summary>A button, a date, or a sentence ending in a colon: nothing to buy and nothing to head.</summary>
    private static bool IsLeftByThePage(string words)
    {
        if (Buttons.Contains(words)) return true;
        if (words.Length >= ShortestDate && words.All(c => char.IsDigit(c) || c is '/' or '-' or '.')) return true;
        return words.EndsWith(':') && words.Length > LongestHeading && !words.Any(char.IsDigit);
    }

    /// <summary>Every letter a capital, and enough of them to be a word.</summary>
    private static bool InCapitals(string words) =>
        words.Count(char.IsLetter) >= ShortestCapitals && !words.Any(char.IsLower) && !words.Any(char.IsDigit);
}
