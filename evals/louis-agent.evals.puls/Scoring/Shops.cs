using System.Text.RegularExpressions;

namespace LouisAgent.Evals.Puls.Scoring;

/// <summary>
/// The shops a cook's list prices at (F31, F34): their names as a column header writes them and their sites, and the facts
/// the shops' tools answer with (which store answered, whether a deal is named, whether a catalogue is there).
/// </summary>
public static partial class Shops
{
    /// <summary>The shops by the name a column header uses, and their sites. The first name a header contains wins.</summary>
    public static readonly IReadOnlyList<(string Name, string Host)> Hosts =
    [
        ("checkers", "checkers.co.za"), ("shoprite", "shoprite.co.za"), ("pick n pay", "pnp.co.za"), ("pnp", "pnp.co.za"),
        ("spar", "spar.co.za"), ("usave", "usave.co.za"), ("woolworths", "woolworths.co.za"),
    ];

    /// <summary>Where a branch is looked up (the cook's OpenStreetMap search): a map, not a source of the answer.</summary>
    public static readonly IReadOnlyList<string> MapHosts = ["openstreetmap.org"];

    /// <summary>The shops the cook has a tool for, by their sites (Checkers today).</summary>
    public static readonly IReadOnlyList<string> ToolShopHosts = ["checkers.co.za"];

    /// <summary>The shop tools, and the two that name a deal.</summary>
    public static readonly IReadOnlyList<string> ShopTools = ["FindProducts", "SearchSpecials", "SearchPromotions"];
    public static readonly IReadOnlyList<string> DealTools = ["SearchSpecials", "SearchPromotions"];

    /// <summary>The site of the shop a column names (Checkers gives checkers.co.za), or null for a column that names no shop.</summary>
    public static string? ShopOf(string header)
    {
        string name = header.ToLowerInvariant();
        return Hosts.FirstOrDefault(shop => name.Contains(shop.Name, StringComparison.Ordinal)).Host;
    }

    /// <summary>How many of the list's rows name a deal beside a linked price (`· BUY 2 FOR R45`, `SAVE`).</summary>
    public static int DealsNamed(string? answer) =>
        Lists.Of(answer).Sum(list => list.Rows.Count(cells =>
            cells.Skip(1).Any(cell => Links.InlineLink().IsMatch(cell) && Deal().IsMatch(cell))));

    /// <summary>How many shops the list prices at that the cook has no tool for (0 is right).</summary>
    public static int UntooledShops(string? answer)
    {
        var shops = Lists.Of(answer).SelectMany(list => list.Header).Select(ShopOf).ToHashSet();
        return shops.Count(shop => shop is not null && !ToolShopHosts.Contains(shop));
    }

    /// <summary>How many shop searches the store serving the user's town answered (the tool names the store that answered).</summary>
    public static int StoreForArea(IEnumerable<string?> results) => results.Count(result => StoreServing().IsMatch(result ?? ""));

    /// <summary>How many shop searches the shop's default store answered: no town, none served, or the town's not used.</summary>
    public static int StoreDefault(IEnumerable<string?> results) => results.Count(result => StoreDefaulted().IsMatch(result ?? ""));

    /// <summary>Whether the answer says no shop near the user has a catalogue here yet, in either language.</summary>
    public static bool NoCatalogueLine(string? answer) => NoCatalogue().IsMatch(answer ?? "");

    [GeneratedRegex(@"\b(BUY \d+ FOR|SAVE)\b|\)\s*·\s*\S", RegexOptions.IgnoreCase)]
    private static partial Regex Deal();

    [GeneratedRegex(@"^(?:Products|Specials \(multi-buys\)|Promotions) at [^(]+\(the store serving ")]
    private static partial Regex StoreServing();

    [GeneratedRegex(@"^(?:Products|Specials \(multi-buys\)|Promotions) at [^(]+\(its default store")]
    private static partial Regex StoreDefaulted();

    [GeneratedRegex(@"No shop near you has a catalogue|Geen winkel naby jou het nog 'n katalogus", RegexOptions.IgnoreCase)]
    private static partial Regex NoCatalogue();
}
