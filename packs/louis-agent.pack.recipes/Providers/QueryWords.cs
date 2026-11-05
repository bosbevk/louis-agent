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

    /// <summary>Pap en sous by every name it goes by: a page may print any of them and none of the others.</summary>
    private static readonly string[] PapEnSous =
    [
        "pap en sous", "pap and sous", "pap sous", "pap met sous", "pap and sheba", "pap sheba", "sheba", "ushatini",
        "tomato and onion gravy", "tomato onion gravy", "tomato gravy"
    ];

    /// <summary>The names of pap en sous worth another search, because most of our sites use them instead.</summary>
    private static readonly string[] PapEnSousToTry = ["pap and sheba", "ushatini", "tomato and onion gravy"];

    /// <summary>
    /// Dishes with names that share no word: a site printing <c>Milk tart</c> has a melktert, and without these it was
    /// dropped, which took melktert from ten sites to six.
    /// </summary>
    private static readonly string[][] TwoNames =
        [["melktert", "milk tart"], ["hoenderpastei", "chicken pie"], PapEnSous];

    /// <summary>
    /// Other names to try for a dish most sites call something else. A site's own search only finds the word it's
    /// given, so the cook has to be told: a filter can't bring back a page the site never returned.
    /// </summary>
    public static IReadOnlyList<string> AlsoCalled(string query)
    {
        string[] words = Words(query);
        return PapEnSous.Any(name => Holds(words, name))
            ? [.. PapEnSousToTry.Where(name => !Holds(words, name))]
            : [];
    }

    /// <summary>
    /// Whether a hit answers the search. The address counts for its path alone, or every page of Jan Braai, at
    /// braai.com, would answer "braai". A search with no real word in it can't tell, so it keeps the hit.
    /// </summary>
    public static bool Answers(string title, Uri page, string query)
    {
        var wanted = Words(query).Where(word => !Joining.Contains(word)).ToList();
        if (wanted.Count == 0) return true;

        var found = Words(title).Concat(Words(Uri.UnescapeDataString(page.AbsolutePath))).ToList();
        return wanted.Any(word => found.Any(other => Same(word, other))) || IsTheOtherName(Words(query), found);
    }

    /// <summary>The search names a dish and the hit its other name, whole: an apple tart is no melktert.</summary>
    private static bool IsTheOtherName(IEnumerable<string> query, IEnumerable<string> found) =>
        TwoNames.Any(names => names.Any(name => Holds(query, name)) && names.Any(name => Holds(found, name)));

    /// <summary>Whether a name stands in these words, as words of their own.</summary>
    private static bool Holds(IEnumerable<string> words, string name) =>
        $" {string.Join(' ', words)} ".Contains($" {name} ", StringComparison.OrdinalIgnoreCase);

    /// <summary>The same word, or one holding the other when the shorter is long enough: potjie, potjiekos.</summary>
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
