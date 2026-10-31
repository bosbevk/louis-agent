using LouisAgent.Evals.Markdown;

namespace LouisAgent.Packs.Shops.Checks;

/// <summary>
/// The shops an answer's list can price at: their names as a column header writes them, their sites, and the ones
/// the agent can read through this pack (Checkers through its shop tools, PnP and Woolworths as a rendered page).
/// </summary>
/// <remarks>
/// Moved from Puls's <c>Shops</c> with <see cref="UntooledShops"/> (F49.3 step 1b), the rest of the shop knowledge
/// following in step 2. Puls keeps its own copy of <see cref="Hosts"/> for its price reading, since an identity may
/// not reference a pack (ADR 0027); the composition's <c>ShopTableTests</c> holds the two equal (Louis's pick). Named
/// for what it holds, since a class <c>Shops</c> would read as the pack's namespace.
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

    /// <summary>
    /// The shops the agent can read, by their sites: Checkers through its shop tools, PnP and Woolworths as a rendered
    /// page.
    /// </summary>
    public static readonly IReadOnlyList<string> ToolShopHosts = ["checkers.co.za", "pnp.co.za", "woolworths.co.za"];

    /// <summary>Which shop's site a column is about: a "Checkers" column is checkers.co.za.</summary>
    public static string? ShopOf(string header)
    {
        string name = header.ToLowerInvariant();
        return Hosts.FirstOrDefault(shop => name.Contains(shop.Name, StringComparison.Ordinal)).Host;
    }

    /// <summary>
    /// The shops the list has a column for that the agent can't read, by their sites, each once in the order they
    /// first appear. None is right.
    /// </summary>
    public static IReadOnlyList<string> UntooledShops(string? answer) =>
    [
        .. Lists.Of(answer).SelectMany(list => list.Header).Select(ShopOf).OfType<string>()
            .Where(shop => !ToolShopHosts.Contains(shop)).Distinct(StringComparer.Ordinal)
    ];
}
