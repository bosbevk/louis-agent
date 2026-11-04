using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Puls.Admin.Explorer.Rings;
using LouisAgent.Puls.Admin.Explorer.Tree;

namespace LouisAgent.Puls.Tests.Admin.Explorer.Rings;

/// <summary>The sites a mode read, sorted for the sources ring, and the commonest reason its tools failed.</summary>
public class SiteCountsTests
{
    /// <summary>A shop (or its subdomain), another South African site, the map and the rest, each site once.</summary>
    [Test]
    public void TheSites_AreSortedByKind_EachOnce()
    {
        var sites = SiteCounts.Of([
            "www.checkers.co.za", "pnp.co.za", "food.co.za", "www.openstreetmap.org",
            "example.com", "pnp.co.za", "notpnp.co.za"
        ]);

        Assert.That(sites, Is.EqualTo(new SiteCounts(sites.Hosts, 2, 2, 1, 1)));
        Assert.That(sites.Hosts, Has.Count.EqualTo(6));
    }

    /// <summary>The commonest reason over the turns, the same one added up; none when nothing failed.</summary>
    [Test]
    public void TheCommonestFailure_IsAddedUpOverTheTurns()
    {
        TurnNode[] turns =
        [
            new()
            {
                ToolReasons =
                    [new ToolReasonRow("Fetch", "timed out", 1), new ToolReasonRow("Search", "refused", 2)]
            },
            new() { ToolReasons = [new ToolReasonRow("Fetch", "timed out", 2)] }
        ];

        Assert.That(ToolFailure.Commonest(turns), Is.EqualTo(new ToolFailure("Fetch", "timed out", 3)));
        Assert.That(ToolFailure.Commonest([new TurnNode()]), Is.Null);
    }
}
