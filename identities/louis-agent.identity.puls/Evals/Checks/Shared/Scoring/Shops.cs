using LouisAgent.Evals.Markdown;

namespace LouisAgent.Identities.Puls.Evals.Checks.Shared.Scoring;

/// <summary>
/// The shops a cook's list prices at (F31, F34): their names as a column header writes them, their sites, and the
/// tools that read a shop, for Puls's price reading and for <c>priced-at-shop</c> until it moves.
/// </summary>
/// <remarks>
/// The shop rules have moved to the shops pack with what they read (F49.3: <c>untooled-shops</c> in step 1b,
/// <c>estimates-at-shop</c> and the lines it looks for in step 3), and <c>priced-at-shop</c> follows in step 4.
/// <see cref="Hosts"/> stays here for Puls's price reading as a copy of the pack's <c>ShopSites.Hosts</c>, since an
/// identity may not reference a pack (ADR 0027); the composition's <c>ShopTableTests</c> holds the two equal
/// (Louis's pick).
/// </remarks>
public static class Shops
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
}
