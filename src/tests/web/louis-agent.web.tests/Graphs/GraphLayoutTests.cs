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

    /// <summary>
    /// A column's boxes stand where their neighbours are (FX85, Louis: "lines may not overlap"): a box at the bottom
    /// of its column whose arrow goes to the top of the next moves up, so the arrows between the columns don't cross.
    /// </summary>
    [Test]
    public void AColumn_IsOrdered_SoArrowsDontCross()
    {
        var spec = new GraphSpec(
            [
                new GraphNode("a", "A", null, 0), new GraphNode("b", "B", null, 0), new GraphNode("c", "C", null, 0),
                new GraphNode("top", "Top", null, 1), new GraphNode("bottom", "Bottom", null, 1),
            ],
            [new GraphEdge("a", "top"), new GraphEdge("b", "bottom"), new GraphEdge("c", "top")]);

        var placed = GraphLayout.Place(spec);

        var first = placed.Nodes.Where(node => node.Node.Column == 0).OrderBy(node => node.Y).Select(node => node.Node.Id);
        Assert.That(first, Is.EqualTo(new[] { "a", "c", "b" }), "c, which goes to the top, comes before b, which goes to the bottom");
    }

    /// <summary>
    /// A column whose data order already has no crossing keeps that order (FX85, Louis's screenshot: the pipeline's
    /// boxes had moved although their arrows never crossed): the engine's six middleware stay in the order given.
    /// </summary>
    [Test]
    public void AColumnWithoutCrossings_KeepsTheDataOrder()
    {
        var spec = new GraphSpec(
            [
                new GraphNode("loop", "Loop", null, 0), new GraphNode("usage", "Usage", null, 0),
                new GraphNode("cutoff", "Cut-off", null, 0), new GraphNode("tracing", "Tracing", null, 0),
                new GraphNode("replay", "Replay", null, 0), new GraphNode("caching", "Caching", null, 0),
                new GraphNode("tool", "Tool", null, 1), new GraphNode("ledger", "Ledger", null, 1), new GraphNode("model", "Model", null, 1),
            ],
            [new GraphEdge("loop", "tool"), new GraphEdge("usage", "ledger"), new GraphEdge("caching", "model")]);

        var placed = GraphLayout.Place(spec);

        var pipeline = placed.Nodes.Where(node => node.Node.Column == 0).OrderBy(node => node.Y).Select(node => node.Node.Id);
        Assert.That(pipeline, Is.EqualTo(new[] { "loop", "usage", "cutoff", "tracing", "replay", "caching" }));
    }

    /// <summary>
    /// An arrow back to an earlier column loops under the drawing (FX85, Louis: "shouldn't here be a line back, a
    /// specialist is an agent session"): from the box's bottom middle, along a lane below the lowest box, up into the
    /// target's bottom middle; the drawing grows to hold the lane.
    /// </summary>
    [Test]
    public void AnArrowBack_LoopsUnderTheDrawing()
    {
        var spec = new GraphSpec(
            [new GraphNode("who", "Who", null, 0), new GraphNode("part", "Part", null, 1), new GraphNode("session", "Session", null, 2)],
            [new GraphEdge("who", "part"), new GraphEdge("part", "session"), new GraphEdge("session", "who", "a session of it")]);

        var placed = GraphLayout.Place(spec);

        var who = placed.Nodes.Single(node => node.Node.Id == "who");
        var session = placed.Nodes.Single(node => node.Node.Id == "session");
        var back = placed.Edges.Single(edge => edge.Edge.To == "who");
        Assert.Multiple(() =>
        {
            Assert.That(back.Path, Does.StartWith($"M {session.X + session.W / 2:0.#} {session.Y + session.H:0.#} C"));
            Assert.That(back.Path, Does.EndWith($"{who.X + who.W / 2:0.#} {who.Y + who.H:0.#}"));
            Assert.That(placed.Height, Is.GreaterThan(placed.Nodes.Max(node => node.Y + node.H) + 20), "room for the lane");
            Assert.That(back.WordY, Is.GreaterThan(placed.Nodes.Max(node => node.Y + node.H)), "the word sits on the lane");
        });
    }

    /// <summary>An arrow from a box to itself loops out of its right side (a specialist may call another specialist).</summary>
    [Test]
    public void AnArrowToItself_LoopsOutOfTheRightSide()
    {
        var spec = new GraphSpec(
            [new GraphNode("a", "A", null, 0), new GraphNode("b", "B", null, 1)],
            [new GraphEdge("a", "b"), new GraphEdge("b", "b", "calls another")]);

        var placed = GraphLayout.Place(spec, 880);

        var b = placed.Nodes.Single(node => node.Node.Id == "b");
        var loop = placed.Edges.Single(edge => edge.Edge.From == "b" && edge.Edge.To == "b");
        Assert.Multiple(() =>
        {
            Assert.That(loop.Path, Does.StartWith($"M {b.X + b.W:0.#} "));
            Assert.That(loop.Path, Does.EndWith($"{b.X + b.W:0.#} {b.Y + b.H * 0.7:0.#}"));
            Assert.That(loop.WordX, Is.GreaterThan(b.X + b.W), "the word sits beside the box");
            Assert.That(placed.Width, Is.GreaterThan(880), "the drawing widens for a loop on the last column");
        });
    }

    /// <summary>A word sits in the middle of its arrow, the empty ground between two columns, so two words leaving one box part.</summary>
    [Test]
    public void AWord_SitsBetweenTheColumns()
    {
        var placed = GraphLayout.Place(TwoColumns());

        var left = placed.Nodes.Single(node => node.Node.Id == "who");
        var right = placed.Nodes.Single(node => node.Node.Id == "packs");
        var edge = placed.Edges.Single(edge => edge.Edge.To == "packs");
        double start = left.X + left.W, gap = right.X - start;
        Assert.That(edge.WordX, Is.GreaterThan(start + gap * 0.4).And.LessThan(start + gap * 0.6));
    }

    /// <summary>
    /// Columns never touch (FX85, Louis's screenshot of a five-column diagram): when the width can't hold them with
    /// room between, the drawing grows and the page scales it, so a box is never drawn over another.
    /// </summary>
    [Test]
    public void ManyColumns_NeverOverlap()
    {
        var spec = new GraphSpec(Enumerable.Range(0, 6).Select(column => new GraphNode($"n{column}", $"N{column}", null, column)).ToList(), []);

        var placed = GraphLayout.Place(spec, 880);

        var boxes = placed.Nodes.OrderBy(node => node.X).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(placed.Width, Is.GreaterThan(880), "the drawing widens");
            for (int at = 1; at < boxes.Count; at++)
                Assert.That(boxes[at].X - (boxes[at - 1].X + boxes[at - 1].W), Is.GreaterThanOrEqualTo(60), "room between columns");
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
