using LouisAgent.Web.Evals.Admin.Explorer.Sites;
using LouisAgent.Puls.Content;

namespace LouisAgent.Puls.Tests.Content;

/// <summary>How Puls sorts the websites its agent reads.</summary>
public class PulsSitesTests
{
    /// <summary>A shop or its subdomain, another South African site, the map and the rest, each site once.</summary>
    [Test]
    public void TheSites_AreSortedLikeThis()
    {
        var sites = SiteCounts.Of([
            "www.checkers.co.za", "pnp.co.za", "food.co.za", "www.openstreetmap.org", "example.com", "pnp.co.za",
            "notpnp.co.za"
        ], PulsSites.Kinds);

        Assert.That(sites.Kinds, Is.EqualTo(new[] { 2, 2, 1 }));
        Assert.That(sites.Rest, Is.EqualTo(1));
    }
}
