using LouisAgent.Web.Graphs;

namespace LouisAgent.Web.Tests.Graphs;

/// <summary>
/// The layout gives a diagram's boxes and arrows their places (F111): columns left to right, a column's boxes centred,
/// an arrow from a box's right edge to the next box's left edge, and a height that fits the tallest column.
/// </summary>
public class GraphLayoutTests
{
    /// <summary>One box on the left, three on the right, three arrows: the Identities page's shape.</summary>
    private static GraphSpec TwoColumns() => new(
        [
            new GraphNode("who", "An identity", "who the agent is", 0),
            new GraphNode("voice", "A voice", null, 1),
            new GraphNode("roles", "Roles", null, 1),
            new GraphNode("packs", "The packs it names", null, 1),
        ],
        [new GraphEdge("who", "voice"), new GraphEdge("who", "roles"), new GraphEdge("who", "packs", "names")]);

    /// <summary>The first column starts at the left edge and the last ends at the right edge.</summary>
    [Test]
    public void TheColumns_SpanTheWidth()
    {
        var placed = GraphLayout.Place(TwoColumns(), 880);

        var left = placed.Nodes.Single(node => node.Node.Id == "who");
        var right = placed.Nodes.Single(node => node.Node.Id == "voice");
        Assert.Multiple(() =>
        {
            Assert.That(placed.Width, Is.EqualTo(880));
            Assert.That(left.X, Is.EqualTo(0));
            Assert.That(right.X + right.W, Is.EqualTo(880));
        });
    }

    /// <summary>A column's boxes are stacked with a gap and the shorter column is centred on the taller one.</summary>
    [Test]
    public void AColumn_IsStackedAndCentred()
    {
        var placed = GraphLayout.Place(TwoColumns());

        var right = placed.Nodes.Where(node => node.Node.Column == 1).OrderBy(node => node.Y).ToList();
        var left = placed.Nodes.Single(node => node.Node.Id == "who");
        Assert.Multiple(() =>
        {
            Assert.That(right[0].Y, Is.EqualTo(0));
            Assert.That(right[1].Y, Is.GreaterThan(right[0].Y + right[0].H), "a gap between boxes");
            Assert.That(right[2].Y + right[2].H, Is.EqualTo(placed.Height), "the tallest column is the height");
            Assert.That(left.Y + left.H / 2, Is.EqualTo(placed.Height / 2.0).Within(0.5), "the one box sits in the middle");
        });
    }

    /// <summary>A box with a note is taller than one without.</summary>
    [Test]
    public void ABoxWithANote_IsTaller()
    {
        var placed = GraphLayout.Place(TwoColumns());

        Assert.That(placed.Nodes.Single(node => node.Node.Id == "who").H,
            Is.GreaterThan(placed.Nodes.Single(node => node.Node.Id == "voice").H));
    }

    /// <summary>An arrow leaves the left box's right edge at its middle and arrives at the right box's left edge.</summary>
    [Test]
    public void AnArrow_RunsFromRightEdgeToLeftEdge()
    {
        var placed = GraphLayout.Place(TwoColumns());

        var left = placed.Nodes.Single(node => node.Node.Id == "who");
        var right = placed.Nodes.Single(node => node.Node.Id == "packs");
        var edge = placed.Edges.Single(edge => edge.Edge.To == "packs");
        Assert.Multiple(() =>
        {
            Assert.That(edge.Path, Does.StartWith($"M {left.X + left.W:0.#} {left.Y + left.H / 2:0.#} C"));
            Assert.That(edge.Path, Does.EndWith($"{right.X:0.#} {right.Y + right.H / 2:0.#}"));
            Assert.That(edge.Length, Is.GreaterThan(right.X - (left.X + left.W)), "a curve is longer than the gap");
            Assert.That(edge.Edge.Word, Is.EqualTo("names"));
        });
    }

    /// <summary>An arrow to a box the diagram doesn't have is left out, not drawn to nowhere.</summary>
    [Test]
    public void AnArrowToNowhere_IsLeftOut()
    {
        var spec = new GraphSpec([new GraphNode("a", "A", null, 0)], [new GraphEdge("a", "missing")]);

        Assert.That(GraphLayout.Place(spec).Edges, Is.Empty);
    }

    /// <summary>One column alone stands in the middle of the width.</summary>
    [Test]
    public void OneColumn_StandsInTheMiddle()
    {
        var spec = new GraphSpec([new GraphNode("a", "A", null, 0)], []);

        var placed = GraphLayout.Place(spec, 880);

        var box = placed.Nodes.Single();
        Assert.That(box.X + box.W / 2, Is.EqualTo(440));
    }
}
