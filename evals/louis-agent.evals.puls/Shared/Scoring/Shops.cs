using System.Text.RegularExpressions;

namespace LouisAgent.Evals.Puls.Shared.Scoring;

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

    /// <summary>The shops the cook can read, by their sites: Checkers with its shop tools, PnP and Woolworths as a rendered page (ReadRenderedPage).</summary>
    public static readonly IReadOnlyList<string> ToolShopHosts = ["checkers.co.za", "pnp.co.za", "woolworths.co.za"];

    /// <summary>The tool that reads a shop with no catalogue: a page of its site, rendered.</summary>
    public const string PageTool = "ReadRenderedPage";

    /// <summary>The shop tools, and the two that name a deal.</summary>
    public static readonly IReadOnlyList<string> ShopTools = ["FindProducts", "SearchSpecials", "SearchPromotions"];
    public static readonly IReadOnlyList<string> DealTools = ["SearchSpecials", "SearchPromotions"];

    /// <summary>The site of the shop a column names (Checkers gives checkers.co.za), or null for a column that names no shop.</summary>
    public static string? ShopOf(string header)
    {
        string name = header.ToLowerInvariant();
        return Hosts.FirstOrDefault(shop => name.Contains(shop.Name, StringComparison.Ordinal)).Host;
    }

    /// <summary>The shop site a URL is on (a subdomain counts), or null when it is on none of them.</summary>
    public static string? SiteOf(string url) => Hosts.Select(shop => shop.Host).Distinct().FirstOrDefault(site => Links.IsHost(url, site));

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

    /// <summary>
    /// Whether the answer says its prices are estimates, not checked at a shop, in either language: the line the cook writes under its list when the user
    /// chose no shop (`any`), in place of the tally.
    /// </summary>
    public static bool EstimatesLine(string? answer) => Estimates().IsMatch(answer ?? "");

    /// <summary>Whether the answer says the shop has no branch in the town ("no PnP branch", "geen PnP-tak"), in either language.</summary>
    public static bool NoBranchLine(string? answer) => NoBranch().IsMatch(answer ?? "");

    /// <summary>
    /// Whether the answer names the shop's branch under Where to buy (F18 step 5.7), in either language: the shop's name (<c>Pick n Pay</c>, <c>PnP</c>) and then a
    /// place (<c>Pick n Pay Malmesbury</c>, <c>PnP in Paarl Mall</c>, <c>Pick n Pay se tak in Malmesbury</c>). A shop with no place after it, a name only in a link,
    /// or another shop's branch is none, and so is a branch named outside the section.
    /// </summary>
    public static bool NamesBranch(string? answer, string shop)
    {
        string host = Hosts.First(known => known.Name == shop).Host;
        string names = string.Join("|", Hosts.Where(known => known.Host == host).Select(known => Regex.Escape(known.Name)));
        string section = InlineLinkText().Replace(WhereToBuySection(answer), " ").Replace("*", "");
        foreach (Match name in Regex.Matches(section, $@"\b(?:{names})\b", RegexOptions.IgnoreCase))
        {
            string rest = section[(name.Index + name.Length)..].Split('\n')[0];
            string? place = Regex.Split(rest, @"[\s|,:;()\-–.]+").FirstOrDefault(token => token.Length > 0 && !BranchWords.Contains(token.ToLowerInvariant()));
            if (place is { Length: > 0 } && char.IsUpper(place[0]) && !Hosts.Any(known => known.Name.StartsWith(place, StringComparison.OrdinalIgnoreCase)))
                return true;
        }
        return false;
    }

    /// <summary>The words between a shop's name and its place that name no place ("PnP in Paarl", "Pick n Pay se tak in Malmesbury").</summary>
    private static readonly HashSet<string> BranchWords = ["in", "te", "by", "at", "near", "naby", "se", "tak", "branch", "op", "website", "shop", "winkel"];

    /// <summary>The lines under the Where to buy heading (<c>Waar om te koop</c>), up to the next heading; none when there is no such heading.</summary>
    private static string WhereToBuySection(string? answer)
    {
        var lines = (answer ?? "").Split('\n');
        int start = Array.FindIndex(lines, line => WhereToBuyHeading().IsMatch(line));
        return start < 0 ? "" : string.Join('\n', lines.Skip(start + 1).TakeWhile(line => !line.TrimStart().StartsWith('#')));
    }

    [GeneratedRegex(@"^\s*#{1,6}\s*(?:where to buy|waar om te koop)\b", RegexOptions.IgnoreCase)]
    private static partial Regex WhereToBuyHeading();

    [GeneratedRegex(@"\[[^\]]*\]\([^)]*\)|https?://\S+")]
    private static partial Regex InlineLinkText();

    /// <summary>Whether the answer says no shop near the user has a catalogue here yet, in either language.</summary>
    public static bool NoCatalogueLine(string? answer) => NoCatalogue().IsMatch(answer ?? "");

    [GeneratedRegex(@"\b(BUY \d+ FOR|SAVE)\b|\)\s*·\s*\S", RegexOptions.IgnoreCase)]
    private static partial Regex Deal();

    [GeneratedRegex(@"^(?:Products|Specials \(multi-buys\)|Promotions) at [^(]+\(the store serving ")]
    private static partial Regex StoreServing();

    [GeneratedRegex(@"^(?:Products|Specials \(multi-buys\)|Promotions) at [^(]+\(its default store")]
    private static partial Regex StoreDefaulted();

    [GeneratedRegex(@"Prices are estimates|Pryse is skattings", RegexOptions.IgnoreCase)]
    private static partial Regex Estimates();

    [GeneratedRegex(@"\b(?:no|geen)\b[^.\n|]{0,40}\b(?:branch|tak)\b", RegexOptions.IgnoreCase)]
    private static partial Regex NoBranch();

    [GeneratedRegex(@"No shop near you has a catalogue|Geen winkel naby jou het nog 'n katalogus", RegexOptions.IgnoreCase)]
    private static partial Regex NoCatalogue();
}
