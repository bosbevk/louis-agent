namespace LouisAgent.Packs.Recipes.Providers;

/// <summary>
/// Tells a real search result from a site's suggestions. Three sites answer a search with no results by listing other
/// recipes, and those read like hits: Jan Braai gave <c>Puttanesca</c> for "pap and sous". And one word of a dish's
/// name isn't the dish: "sous vide" is no pap en sous, and offering it sent the cook searching again. A hit answers a
/// search when its title or its address holds the dish's whole name.
/// </summary>
/// <remarks>
/// A plain word walk before a regex (ADR 0057). A plural or a longer name still counts, a search of three words or more
/// can miss one, since the cook may add a word no page has, and a dish's other names count whole. A real recipe with
/// another title can still be missed, which is what the line of other names to try is for.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0057-regex-is-the-last-option.md">ADR 0057: regex is the last option</seealso>
internal static class QueryWords
{
    /// <summary>Words that only join others, say where a dish is from, or praise it: no title need hold them.</summary>
    private static readonly HashSet<string> Joining = new(
        ("and the with for recipe recipes south african en met vir die resep resepte suid afrikaanse " +
         "traditional tradisionele easy maklike quick vinnige best beste classic klassieke homemade lekker").Split(' '),
        StringComparer.OrdinalIgnoreCase);

    /// <summary>Under this length a word must match whole: <c>pap</c> is no <c>paprika</c>.</summary>
    private const int ShortestPart = 4;

    /// <summary>A search of up to this many real words is the dish's name, so a hit holds them all.</summary>
    private const int WordsInAName = 2;

    /// <summary>Pap en sous by every name it goes by: a page may print any of them and none of the others.</summary>
    private static readonly string[] PapEnSous =
    [
        "pap en sous", "pap and sous", "pap sous", "pap met sous", "pap and sheba", "pap sheba", "sheba", "ushatini",
        "tomato and onion gravy", "tomato onion gravy", "tomato gravy"
    ];

    /// <summary>The names of pap en sous worth another search, because most of our sites use them instead.</summary>
    private static readonly string[] PapEnSousToTry = ["pap and sheba", "ushatini", "tomato and onion gravy"];

    /// <summary>
    /// Dishes with names that share no word, in the other language or another spelling: a site printing
    /// <c>Milk tart</c> has a melktert, and without these it was dropped, which took melktert from ten sites to six.
    /// </summary>
    private static readonly string[][] TwoNames =
    [
        ["melktert", "milk tart"], ["hoenderpastei", "chicken pie"], ["appeltert", "apple tart"],
        ["ingelegde vis", "pickled fish"], ["karringmelkbeskuit", "karringmelk beskuit", "buttermilk rusks"],
        ["pampoenkoekies", "pumpkin fritters"], ["beesstert", "oxtail"], ["sjokoladekoek", "chocolate cake"],
        ["wortelkoek", "carrot cake"], ["botterskorsiesop", "butternut soup"], ["bananabrood", "banana bread"],
        ["malvapoeding", "malva pudding"], ["souskluitjies", "cinnamon dumplings"], ["skons", "scones"],
        ["frikkadelle", "frikkadels"], ["hertzoggies", "herzoggies"], ["samoosas", "samosas", "samoosa", "samosa"],
        ["macaroni and cheese", "macaroni cheese", "mac and cheese", "macaroni kaas"], PapEnSous
    ];

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
    /// What else to search for when a search found nothing: the dish's other names, so the empty result gives the next
    /// search a word. A result with hits only gets <see cref="AlsoCalled"/>, as it has something to read already.
    /// </summary>
    public static IReadOnlyList<string> OtherNames(string query)
    {
        string[] words = Words(query);
        var names = TwoNames.FirstOrDefault(names => names.Any(name => Holds(words, name))) ?? [];
        return names == PapEnSous ? AlsoCalled(query) : [.. names.Where(name => !Holds(words, name))];
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
        int held = wanted.Count(word => found.Any(other => Same(word, other)));
        int enough = wanted.Count <= WordsInAName ? wanted.Count : wanted.Count - 1;
        return held >= enough || IsTheOtherName(Words(query), found);
    }

    /// <summary>The search names a dish and the hit its other name, whole: an apple tart is no melktert.</summary>
    private static bool IsTheOtherName(IEnumerable<string> query, IEnumerable<string> found) =>
        TwoNames.Any(names => names.Any(name => Holds(query, name)) && names.Any(name => Holds(found, name)));

    /// <summary>Whether a name stands in these words, as words of their own.</summary>
    private static bool Holds(IEnumerable<string> words, string name) =>
        $" {string.Join(' ', words)} ".Contains($" {name} ", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The same word, its plural, or one holding the other when the shorter is long enough: pie, pies; potjie,
    /// potjiekos.
    /// </summary>
    private static bool Same(string one, string other)
    {
        var (shorter, longer) = one.Length <= other.Length ? (one, other) : (other, one);
        return shorter.Length < ShortestPart
            ? shorter.TrimEnd('s', 'S').Equals(longer.TrimEnd('s', 'S'), StringComparison.OrdinalIgnoreCase)
            : longer.Contains(shorter, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The runs of letters in a text.</summary>
    private static string[] Words(string text) =>
        new string([.. text.Select(c => char.IsLetter(c) ? c : ' ')])
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);
}
