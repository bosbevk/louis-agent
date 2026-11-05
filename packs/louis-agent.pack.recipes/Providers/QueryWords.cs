namespace LouisAgent.Packs.Recipes.Providers;

/// <summary>
/// Tells a real search result from a site's suggestions. Three sites answer a search with no results by listing other
/// recipes, and those read like hits: Jan Braai gave <c>Puttanesca</c> for "pap and sous". A hit that shares no word
/// with the search, in its title or its address, isn't an answer to it.
/// </summary>
/// <remarks>
/// A plain word walk before a regex (ADR 0057). It's deliberately generous, since the model still reads the titles:
/// its job is to drop what plainly has nothing to do with the search, and a plural or a longer name still counts.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0057-regex-is-the-last-option.md">ADR 0057: regex is the last option</seealso>
internal static class QueryWords
{
    /// <summary>Words that only join others or say where a dish is from, which would match half of any site.</summary>
    private static readonly HashSet<string> Joining = new(
        "and the with for recipe recipes south african met vir die resep resepte suid afrikaanse".Split(' '),
        StringComparer.OrdinalIgnoreCase);

    /// <summary>Under this length a word must match whole: <c>pap</c> is no <c>paprika</c>.</summary>
    private const int ShortestPart = 4;

    /// <summary>
    /// Whether a hit answers the search. The address counts for its path alone, or every page of Jan Braai, at
    /// braai.com, would answer "braai". A search with no real word in it can't tell, so it keeps the hit.
    /// </summary>
    public static bool Answers(string title, Uri page, string query)
    {
        var wanted = Words(query).Where(word => !Joining.Contains(word)).ToList();
        if (wanted.Count == 0) return true;

        var found = Words(title).Concat(Words(Uri.UnescapeDataString(page.AbsolutePath))).ToList();
        return wanted.Any(word => found.Any(other => Same(word, other)));
    }

    /// <summary>The same word, or one holding the other when the shorter is long enough: potjie and potjiekos.</summary>
    private static bool Same(string one, string other)
    {
        var (shorter, longer) = one.Length <= other.Length ? (one, other) : (other, one);
        return shorter.Length < ShortestPart
            ? shorter.Equals(longer, StringComparison.OrdinalIgnoreCase)
            : longer.Contains(shorter, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The runs of letters in a text.</summary>
    private static string[] Words(string text) =>
        new string([.. text.Select(c => char.IsLetter(c) ? c : ' ')])
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);
}
