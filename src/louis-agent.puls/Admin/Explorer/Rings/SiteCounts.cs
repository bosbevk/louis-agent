namespace LouisAgent.Puls.Admin.Explorer.Rings;

/// <summary>
/// The sites a mode read pages from, sorted for its sources ring: South African shops, other South African sites,
/// the map the cook looks branches up on, and the rest.
/// </summary>
public sealed record SiteCounts(IReadOnlyList<string> Hosts, int Shops, int Za, int Maps, int Rest)
{
    /// <summary>The shops the cook prices at; a subdomain of one counts as the shop.</summary>
    private static readonly string[] ShopSites =
        ["checkers.co.za", "pnp.co.za", "woolworths.co.za", "spar.co.za", "shoprite.co.za", "usave.co.za"];

    /// <summary>Sorts the sites read, each counted once.</summary>
    /// <remarks>
    /// F34: a shop search counts as its shop's site, and a branch looked up on a map is its own slice, not a place
    /// prices come from.
    /// </remarks>
    public static SiteCounts Of(IEnumerable<string> hosts)
    {
        var sites = hosts.Distinct().ToList();
        int shops = sites.Count(IsShop), za = sites.Count(h => h.EndsWith(".za") && !IsShop(h));
        int maps = sites.Count(IsMap);
        return new SiteCounts(sites, shops, za, maps, sites.Count - shops - za - maps);
    }

    /// <summary>The host is one of the shops, or a subdomain of one.</summary>
    private static bool IsShop(string host) => ShopSites.Any(s => host == s || host.EndsWith("." + s));

    /// <summary>The host is the map, which says where a branch is, not what anything costs.</summary>
    private static bool IsMap(string host) => host == "openstreetmap.org" || host.EndsWith(".openstreetmap.org");
}
