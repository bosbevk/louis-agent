using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Web.Admin.Explorer.Rings;
using LouisAgent.Web.Admin.Explorer.Sites;
using LouisAgent.Web.Admin.Explorer.Tree;

namespace LouisAgent.Web.Tests.Admin.Explorer.Sites;

/// <summary>The sites a mode read, sorted for the sources ring, and the commonest reason its tools failed.</summary>
public class SiteCountsTests
{
    /// <summary>Two kinds of site, the second by how an address ends.</summary>
    private static readonly SiteKinds Kinds = new([new SiteKind("shops", "shop.example"), new SiteKind("near", ".near")]);

    /// <summary>A shop or something under it, one ending the second way, and the rest, each site once.</summary>
    [Test]
    public void TheSites_AreSortedByKind_EachOnce()
    {
        var sites = SiteCounts.Of(["www.shop.example", "shop.example", "food.near", "notshop.example", "shop.example"],
            Kinds);

        Assert.That(sites.Kinds, Is.EqualTo(new[] { 2, 1 }));
        Assert.That(sites.Rest, Is.EqualTo(1));
        Assert.That(sites.Hosts, Has.Count.EqualTo(4));
    }

    /// <summary>A site that fits two kinds counts once, under the first.</summary>
    [Test]
    public void ASiteOfTwoKinds_CountsUnderTheFirst()
    {
        var kinds = new SiteKinds([new SiteKind("shops", "shop.near"), new SiteKind("near", ".near")]);

        Assert.That(SiteCounts.Of(["shop.near"], kinds).Kinds, Is.EqualTo(new[] { 1, 0 }));
    }

    /// <summary>With no kinds named, every site is the rest.</summary>
    [Test]
    public void WithNoKinds_EverySiteIsTheRest()
    {
        var sites = SiteCounts.Of(["a.example", "b.example"], SiteKinds.None);

        Assert.That(sites.Kinds, Is.Empty);
        Assert.That(sites.Rest, Is.EqualTo(2));
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
