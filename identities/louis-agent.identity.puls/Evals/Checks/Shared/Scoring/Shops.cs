using System.Text.RegularExpressions;
using LouisAgent.Evals.Markdown;

namespace LouisAgent.Identities.Puls.Evals.Checks.Shared.Scoring;

/// <summary>
/// The shops a cook's list prices at (F31, F34): their names as a column header writes them, their sites, the ones
/// the cook can read, and what an answer says about them (estimates, no branch, the branch it names).
/// </summary>
public static partial class Shops
{
    /// <summary>
    /// The shops by the name a column header uses, and their sites. The first name a header contains wins.
    /// </summary>
    public static readonly IReadOnlyList<(string Name, string Host)> Hosts =
    [
        ("checkers", "checkers.co.za"), ("shoprite", "shoprite.co.za"), ("pick n pay", "pnp.co.za"),
        ("pnp", "pnp.co.za"),
        ("spar", "spar.co.za"), ("usave", "usave.co.za"), ("woolworths", "woolworths.co.za"),
    ];

    /// <summary>
    /// The shops the cook can read, by their sites: Checkers through its shop tools, PnP and Woolworths as a rendered
    /// page.
    /// </summary>
    public static readonly IReadOnlyList<string> ToolShopHosts = ["checkers.co.za", "pnp.co.za", "woolworths.co.za"];

    /// <summary>The tool that reads a shop with no catalogue: a page of its site, rendered.</summary>
    public const string PageTool = "ReadRenderedPage";

    /// <summary>The tools that search a shop's own catalogue.</summary>
    public static readonly IReadOnlyList<string> ShopTools = ["FindProducts", "SearchSpecials", "SearchPromotions"];

    /// <summary>Which shop's site a column is about: a "Checkers" column is checkers.co.za.</summary>
    public static string? ShopOf(string header)
    {
        string name = header.ToLowerInvariant();
        return Hosts.FirstOrDefault(shop => name.Contains(shop.Name, StringComparison.Ordinal)).Host;
    }

    /// <summary>Which shop's site a link is on, a subdomain included.</summary>
    public static string? SiteOf(string url) =>
        Hosts.Select(shop => shop.Host).Distinct().FirstOrDefault(site => Links.IsHost(url, site));

    /// <summary>
    /// The shops the list has a column for that the cook can't read, by their sites, each once in the order they first
    /// appear. None is right.
    /// </summary>
    public static IReadOnlyList<string> UntooledShops(string? answer) =>
    [
        .. Lists.Of(answer).SelectMany(list => list.Header).Select(ShopOf).OfType<string>()
            .Where(shop => !ToolShopHosts.Contains(shop)).Distinct(StringComparer.Ordinal)
    ];

    /// <summary>
    /// The answer says its prices are estimates, not checked at a shop, in either language: the line the cook writes
    /// under its list when the user chose no shop.
    /// </summary>
    public static bool EstimatesLine(string? answer) => Estimates().IsMatch(answer ?? "");

    /// <summary>The answer says the shop has no branch in the town ("no PnP branch", "geen PnP-tak").</summary>
    public static bool NoBranchLine(string? answer) => NoBranch().IsMatch(answer ?? "");

    /// <summary>
    /// The answer names the shop's branch under Where to buy, in either language (<see cref="BranchReading"/> says
    /// which case it is when it doesn't).
    /// </summary>
    public static bool NamesBranch(string? answer, string shop) =>
        BranchReading.Of(answer, shop) == BranchFinding.Named;

    /// <summary>The estimates line, in English or Afrikaans.</summary>
    [GeneratedRegex(@"Prices are estimates|Pryse is skattings", RegexOptions.IgnoreCase)]
    private static partial Regex Estimates();

    /// <summary>"No" and "branch" close together, in English or Afrikaans, within one sentence or table cell.</summary>
    [GeneratedRegex(@"\b(?:no|geen)\b[^.\n|]{0,40}\b(?:branch|tak)\b", RegexOptions.IgnoreCase)]
    private static partial Regex NoBranch();
}
