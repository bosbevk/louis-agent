namespace LouisAgent.Puls.Admin.Explorer.Sites;

/// <summary>The websites a mode read pages from, counted under the kinds its site names, and the rest.</summary>
/// <param name="Hosts">Every website read, each once.</param>
/// <param name="Kinds">How many fit each kind, in the kinds' order.</param>
/// <param name="Rest">How many fit none.</param>
public sealed record SiteCounts(IReadOnlyList<string> Hosts, IReadOnlyList<int> Kinds, int Rest)
{
    /// <summary>Sorts the websites read, each counted once, under the first kind it fits.</summary>
    public static SiteCounts Of(IEnumerable<string> hosts, SiteKinds kinds)
    {
        var sites = hosts.Distinct().ToList();

        // Which kind each site went under, -1 for none.
        var under = sites.Select(site => kinds.Kinds.ToList().FindIndex(kind => kind.Fits(site))).ToList();

        return new SiteCounts(sites, [.. kinds.Kinds.Select((_, i) => under.Count(kind => kind == i))],
            under.Count(kind => kind < 0));
    }
}
