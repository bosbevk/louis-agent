using LouisAgent.Site.Graphs.Framework;
using LouisAgent.Site.Pages.Framework;

namespace LouisAgent.Site.Tests.Graphs.Framework;

/// <summary>
/// The Identities page's diagram says what the page says (F111): a box for each part the page lists, the three levels
/// the engine knows, every arrow between boxes that exist, and the page shows it once.
/// </summary>
public class IdentityGraphTests
{
    /// <summary>Each part the page lists in bold is a box, by its first word.</summary>
    [Test]
    public void EveryPartThePageLists_IsABox()
    {
        using var ctx = new BunitContext();
        var listed = ctx.Render<IdentitiesPage>().FindAll(".rules li strong")
            .Select(part => part.TextContent.TrimEnd('.').Split(',')[0])
            .Where(part => part != "By level").ToList();

        var labels = IdentityGraph.Spec.Nodes.Select(node => node.Label).ToList();

        Assert.That(listed, Has.Count.EqualTo(5), "the page lists the five parts");
        Assert.That(listed.Where(part => !labels.Contains(part)), Is.Empty);
    }

    /// <summary>The levels are the engine's three: 0 the agent, 1 a specialist, 2 a minion.</summary>
    [Test]
    public void TheLevels_AreTheEngines()
    {
        var levels = IdentityGraph.Spec.Nodes.Where(node => node.Column == 2).Select(node => node.Label).ToList();

        Assert.That(levels, Is.EqualTo(new[] { "Level 0: the agent", "Level 1: a specialist", "Level 2: a minion" }));
    }

    /// <summary>Every arrow joins two boxes the diagram has.</summary>
    [Test]
    public void EveryArrow_JoinsTwoBoxes()
    {
        var ids = IdentityGraph.Spec.Nodes.Select(node => node.Id).ToHashSet();

        Assert.That(IdentityGraph.Spec.Edges.Where(edge => !ids.Contains(edge.From) || !ids.Contains(edge.To)),
            Is.Empty);
    }

    /// <summary>The page shows the diagram once, under its lede.</summary>
    [Test]
    public void ThePage_ShowsIt()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<IdentitiesPage>();

        Assert.That(cut.FindAll("figure.graph"), Has.Count.EqualTo(1));
        Assert.That(cut.FindAll("figure.graph g.node"), Has.Count.EqualTo(IdentityGraph.Spec.Nodes.Count));
    }
}
