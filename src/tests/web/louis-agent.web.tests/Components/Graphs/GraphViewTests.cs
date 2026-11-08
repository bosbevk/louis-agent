using LouisAgent.Web.Components.Graphs;
using LouisAgent.Web.Graphs;

namespace LouisAgent.Web.Tests.Components.Graphs;

/// <summary>
/// The view draws a diagram from its data (F111): one path per arrow, one group per box, a word where an arrow has
/// one, the root marked for the frame's reveal, and the markup pinned to its golden file.
/// </summary>
public class GraphViewTests
{
    /// <summary>The Identities page's shape: one box on the left, three on the right.</summary>
    private static GraphSpec Identity() => new(
        [
            new GraphNode("who", "An identity", "who the agent is", 0),
            new GraphNode("voice", "A voice", null, 1),
            new GraphNode("roles", "Roles", null, 1),
            new GraphNode("packs", "The packs it names", null, 1),
        ],
        [new GraphEdge("who", "voice"), new GraphEdge("who", "roles"), new GraphEdge("who", "packs", "names")]);

    private static IRenderedComponent<GraphView> Render(BunitContext ctx) =>
        ctx.Render<GraphView>(parameters => parameters.Add(view => view.Spec, Identity()).Add(view => view.Title, "An identity, as files"));

    /// <summary>Every box is a group, every arrow a path, the one word on its arrow, the root ready to reveal.</summary>
    [Test]
    public void TheDiagram_HasABoxAndAnArrowForEach()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx);

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll("figure.graph.reveal"), Has.Count.EqualTo(1));
            Assert.That(cut.FindAll("g.node"), Has.Count.EqualTo(4));
            Assert.That(cut.FindAll("path.edge"), Has.Count.EqualTo(3));
            Assert.That(cut.Find("text.word").TextContent, Is.EqualTo("names"));
            Assert.That(cut.Find("figure").GetAttribute("aria-label"), Is.EqualTo("An identity, as files"));
            Assert.That(cut.Find("figcaption").TextContent, Is.EqualTo("An identity, as files"));
        });
    }

    /// <summary>Each box and arrow carries its column, so the styles can delay the columns in turn.</summary>
    [Test]
    public void TheColumns_AreOnTheParts()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx);

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll("g.node").Select(node => node.GetAttribute("style")), Is.EquivalentTo(new[] { "--col: 0", "--col: 1", "--col: 1", "--col: 1" }));
            Assert.That(cut.FindAll("path.edge").Select(edge => edge.GetAttribute("style")), Has.All.Contains("--len: ").And.All.EndsWith("--col: 0"));
        });
    }

    /// <summary>The markup matches the golden file.</summary>
    [Test]
    public void TheDiagram_MatchesTheGoldenFile()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx);

        Snapshots.Match("Components/Graphs/GraphView.html", cut.Markup);
    }
}
