using LouisAgent.Evals.Markdown;

namespace LouisAgent.Identities.Puls.Evals.Checks.Shared.Scoring;

/// <summary>
/// The shops a cook's list prices at (F31, F34): their names as a column header writes them, and their sites, for
/// Puls's price reading.
/// </summary>
/// <remarks>
/// The shop rules are the shops pack's, with what they read (F49.3). <see cref="Hosts"/> stays here for Puls's price
/// reading as a copy of the pack's <c>ShopSites.Hosts</c>, since an identity may not reference a pack (ADR 0027); the
/// composition's <c>ShopTableTests</c> holds the two equal (Louis's pick).
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
