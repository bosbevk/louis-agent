using LouisAgent.Site.Graphs.Framework;
using LouisAgent.Site.Pages.Framework;

namespace LouisAgent.Site.Tests.Graphs.Framework;

/// <summary>
/// The Packs page's diagram says what the page's rules say (F111): only the contracts, the engine's guards, ready for
/// MCP, and a session that loads toolsets; every arrow between boxes that exist, and the page shows it once.
/// </summary>
public class PacksGraphTests
{
    /// <summary>The rules the page lists in bold each have their box or note.</summary>
    [TestCase("only the contracts")]
    [TestCase("The engine's guards")]
    [TestCase("MCP")]
    [TestCase("A session")]
    public void TheRules_AreOnTheDiagram(string words)
    {
        var text = PacksGraph.Spec.Nodes.SelectMany(node => new[] { node.Label, node.Note ?? "" });

        Assert.That(text.Any(part => part.Contains(words)), Is.True, words);
    }

    /// <summary>Every arrow joins two boxes the diagram has.</summary>
    [Test]
    public void EveryArrow_JoinsTwoBoxes()
    {
        var ids = PacksGraph.Spec.Nodes.Select(node => node.Id).ToHashSet();

        Assert.That(PacksGraph.Spec.Edges.Where(edge => !ids.Contains(edge.From) || !ids.Contains(edge.To)), Is.Empty);
    }

    /// <summary>The page shows the diagram once.</summary>
    [Test]
    public void ThePage_ShowsIt()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<PacksPage>();

        Assert.That(cut.FindAll("figure.graph g.node"), Has.Count.EqualTo(PacksGraph.Spec.Nodes.Count));
    }
}
