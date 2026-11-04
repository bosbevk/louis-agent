using LouisAgent.Evals.Markdown;

namespace LouisAgent.Packs.WhereToBuy.Checks.Reading;

/// <summary>
/// The shops an answer's list could name: their names as a column header writes them, their sites, and the tools that
/// would read a shop's page. No shop's site is read any more (ADR 0069), so a price column for any of them is wrong.
/// </summary>
/// <remarks>
/// Moved from Puls's <c>Shops</c> with the rules that read it (F49.3 steps 1b and 3). Puls keeps its own copy of
/// <see cref="Hosts"/> for its price reading, since an identity may not reference a pack (ADR 0027); the
/// composition's <c>ShopTableTests</c> holds the two equal (Louis's pick). Named for what it holds, since a class
/// <c>Shops</c> would read as the pack's namespace.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0027-engine-identities-and-packs.md">ADR 0027: engine, identities and packs</seealso>
public static class ShopSites
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

    /// <summary>The engine's tool that reads a page in the browser.</summary>
    public const string RenderedPageTool = "ReadRenderedPage";

    /// <summary>The engine's tools that read a page, a shop's site among them: rendered, or fetched.</summary>
    public static readonly IReadOnlyList<string> PageTools = [RenderedPageTool, "FetchUrl"];

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
    /// The shops the list has a column for, by their sites, each once in the order they first appear. None is right:
    /// no shop's prices are read.
    /// </summary>
    public static IReadOnlyList<string> UntooledShops(string? answer) =>
    [
        .. Lists.Of(answer).SelectMany(list => list.Header).Select(ShopOf).OfType<string>()
            .Distinct(StringComparer.Ordinal)
    ];
}
