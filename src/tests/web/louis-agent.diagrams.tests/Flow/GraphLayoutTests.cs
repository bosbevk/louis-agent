using LouisAgent.Diagrams.Flow;
using LouisAgent.Diagrams.Flow.Spec;

namespace LouisAgent.Diagrams.Tests.Flow;

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
            new GraphNode("packs", "The packs it names", null, 1)
        ],
        [new GraphEdge("who", "voice"), new GraphEdge("who", "roles"), new GraphEdge("who", "packs", "names")]);

    /// <summary>The first column starts at the left edge and the last ends at the right edge.</summary>
    [Test]
    public void TheColumns_SpanTheWidth()
    {
        var placed = GraphLayout.Place(TwoColumns());

        var left = placed.Nodes.Single(node => node.Node.Id == "who");
        var right = placed.Nodes.Single(node => node.Node.Id == "voice");
        Assert.Multiple(() =>
        {
            Assert.That(placed.Width, Is.EqualTo(880));
            Assert.That(left.X, Is.EqualTo(0));
            Assert.That(right.X + right.W, Is.EqualTo(880));
        });
    }

    /// <summary>
    /// A column's boxes are stacked on the row grid with a gap, a plain box in the middle of its row (the noted box
    /// sets the row), and the shorter column is centred on the taller one.
    /// </summary>
    [Test]
    public void AColumn_IsStackedAndCentred()
    {
        var placed = GraphLayout.Place(TwoColumns());

        var right = placed.Nodes.Where(node => node.Node.Column == 1).OrderBy(node => node.Y).ToList();
        var left = placed.Nodes.Single(node => node.Node.Id == "who");
        double inset = (left.H - right[0].H) / 2;
        Assert.Multiple(() =>
        {
            Assert.That(right[0].Y, Is.EqualTo(inset), "the first row starts at the top, the plain box in its middle");
            Assert.That(right[1].Y, Is.GreaterThan(right[0].Y + right[0].H), "a gap between boxes");
            Assert.That(right[2].Y + right[2].H, Is.EqualTo(placed.Height - inset), "the tallest column is the height");
            Assert.That(left.Y + left.H / 2, Is.EqualTo(placed.Height / 2.0).Within(0.5),
                "the one box sits in the middle");
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

    /// <summary>
    /// An arrow is a connector (FX85, Louis: "make direct lines"): it leaves the middle of the left box's right side
    /// flat, the one stem every arrow out of that box shares, bends at right angles and arrives flat at the middle of
    /// the right box's left side.
    /// </summary>
    [Test]
    public void AnArrow_IsAConnectorFromRightSideToLeftSide()
    {
        var placed = GraphLayout.Place(TwoColumns());

        var left = placed.Nodes.Single(node => node.Node.Id == "who");
        var right = placed.Nodes.Single(node => node.Node.Id == "packs");
        var edge = placed.Edges.Single(edge => edge.Edge.To == "packs");
        Assert.Multiple(() =>
        {
            Assert.That(edge.Path, Does.StartWith($"M {left.X + left.W:0.#} {left.Y + left.H / 2:0.#} L"),
                "out of the middle");
            Assert.That(edge.Path, Does.EndWith($"L {right.X:0.#} {right.Y + right.H / 2:0.#}"));
            Assert.That(edge.Path, Does.Not.Contain(" C "), "no curve");
            Assert.That(edge.Path, Does.Contain(" Q "), "a rounded bend");
            Assert.That(edge.Length, Is.GreaterThan(right.X - (left.X + left.W)),
                "the bends make it longer than the gap");
            Assert.That(edge.Edge.Word, Is.EqualTo("names"));
        });
    }

    /// <summary>
    /// Every arrow between two columns bends at the one track in the middle of the gap (Louis: "conformity"), so arrows
    /// join where curves would have crossed and the bends line up.
    /// </summary>
    [Test]
    public void TheArrowsBetweenTwoColumns_ShareOneTrack()
    {
        var spec = new GraphSpec(
            [
                new GraphNode("a", "A", null, 0), new GraphNode("top", "Top", null, 1),
                new GraphNode("low", "Low", null, 1)
            ],
            [new GraphEdge("a", "top"), new GraphEdge("a", "low")]);

        var placed = GraphLayout.Place(spec);

        // The first bend's corner is on the track: "... Q trackX y ...", the same x for every arrow that bends.
        var bent = placed.Edges.Where(edge => edge.Path.Contains(" Q ")).ToList();
        var tracks = bent.Select(edge => edge.Path.Split(' ').SkipWhile(piece => piece != "Q").Skip(1).First())
            .Distinct().ToList();
        Assert.Multiple(() =>
        {
            Assert.That(bent, Has.Count.EqualTo(2), "both arrows change row");
            Assert.That(tracks, Has.Count.EqualTo(1));
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
                new GraphNode("top", "Top", null, 1), new GraphNode("bottom", "Bottom", null, 1)
            ],
            [new GraphEdge("a", "top"), new GraphEdge("b", "bottom"), new GraphEdge("c", "top")]);

        var placed = GraphLayout.Place(spec);

        var first = placed.Nodes.Where(node => node.Node.Column == 0).OrderBy(node => node.Y)
            .Select(node => node.Node.Id);
        Assert.That(first, Is.EqualTo(new[] { "a", "c", "b" }),
            "c, which goes to the top, comes before b, which goes to the bottom");
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
                new GraphNode("tool", "Tool", null, 1), new GraphNode("ledger", "Ledger", null, 1),
                new GraphNode("model", "Model", null, 1)
            ],
            [new GraphEdge("loop", "tool"), new GraphEdge("usage", "ledger"), new GraphEdge("caching", "model")]);

        var placed = GraphLayout.Place(spec);

        var pipeline = placed.Nodes.Where(node => node.Node.Column == 0).OrderBy(node => node.Y)
            .Select(node => node.Node.Id);
        Assert.That(pipeline, Is.EqualTo(new[] { "loop", "usage", "cutoff", "tracing", "replay", "caching" }));
    }

    /// <summary>
    /// An arrow back to an earlier column runs under the drawing (FX85, Louis: "shouldn't here be a line back, a
    /// specialist is an agent session"): straight down out of the box's bottom middle (CL60.2, Louis's marks on the
    /// runner and MCP charts, in place of FX85's out of the right side), along a lane below the lowest box and up
    /// into the target's bottom; the drawing grows to hold the lane.
    /// </summary>
    [Test]
    public void AnArrowBack_LoopsUnderTheDrawing()
    {
        var spec = new GraphSpec(
            [
                new GraphNode("who", "Who", null, 0), new GraphNode("part", "Part", null, 1),
                new GraphNode("session", "Session", null, 2)
            ],
            [
                new GraphEdge("who", "part"), new GraphEdge("part", "session"),
                new GraphEdge("session", "who", "a session of it")
            ]);

        var placed = GraphLayout.Place(spec);

        var who = placed.Nodes.Single(node => node.Node.Id == "who");
        var session = placed.Nodes.Single(node => node.Node.Id == "session");
        var back = placed.Edges.Single(edge => edge.Edge.To == "who");
        Assert.Multiple(() =>
        {
            Assert.That(back.Path,
                Does.StartWith(
                    $"M {session.X + session.W / 2:0.#} {session.Y + session.H:0.#} L {session.X + session.W / 2:0.#}"),
                "straight down out of the bottom middle");
            Assert.That(back.Path, Does.EndWith($"L {who.X + who.W / 2:0.#} {who.Y + who.H:0.#}"),
                "up into the bottom middle");
            Assert.That(placed.Height, Is.GreaterThan(placed.Nodes.Max(node => node.Y + node.H) + 20),
                "room for the lane");
            Assert.That(back.WordY, Is.GreaterThan(placed.Nodes.Max(node => node.Y + node.H)),
                "the word sits on the lane");
        });
    }

    /// <summary>
    /// An arrow from a box to itself loops out of its right side (a specialist may call another specialist).
    /// </summary>
    [Test]
    public void AnArrowToItself_LoopsOutOfTheRightSide()
    {
        var spec = new GraphSpec(
            [new GraphNode("a", "A", null, 0), new GraphNode("b", "B", null, 1)],
            [new GraphEdge("a", "b"), new GraphEdge("b", "b", "calls another")]);

        var placed = GraphLayout.Place(spec);

        var b = placed.Nodes.Single(node => node.Node.Id == "b");
        var loop = placed.Edges.Single(edge => edge.Edge.From == "b" && edge.Edge.To == "b");
        Assert.Multiple(() =>
        {
            Assert.That(loop.Path, Does.StartWith($"M {b.X + b.W:0.#} "));
            Assert.That(loop.Path, Does.EndWith($"L {b.X + b.W:0.#} {b.Y + b.H * 0.7:0.#}"));
            Assert.That(loop.WordX, Is.GreaterThan(b.X + b.W + 40), "the word sits beside the loop");
            Assert.That(placed.Width, Is.GreaterThan(880), "the drawing widens for a loop on the last column");
            Assert.That(placed.Width, Is.GreaterThanOrEqualTo(loop.WordX + 4 * "calls another".Length),
                "and holds the word");
        });
    }

    /// <summary>
    /// A word sits on a flat piece of its arrow, in the empty ground between the columns, never on a box or on the
    /// shared track.
    /// </summary>
    [Test]
    public void AWord_SitsBetweenTheColumns()
    {
        var placed = GraphLayout.Place(TwoColumns());

        var left = placed.Nodes.Single(node => node.Node.Id == "who");
        var right = placed.Nodes.Single(node => node.Node.Id == "packs");
        var edge = placed.Edges.Single(edge => edge.Edge.To == "packs");
        Assert.Multiple(() =>
        {
            Assert.That(edge.WordX, Is.GreaterThan(left.X + left.W).And.LessThan(right.X));
            Assert.That(edge.WordY, Is.EqualTo(left.Y + left.H / 2).Or.EqualTo(right.Y + right.H / 2),
                "on one of its flat pieces");
        });
    }

    /// <summary>
    /// Columns never touch (FX85, Louis's screenshot of a five-column diagram): when the width can't hold them with
    /// room between, the drawing grows and the page scales it, so a box is never drawn over another.
    /// </summary>
    [Test]
    public void ManyColumns_NeverOverlap()
    {
        var spec = new GraphSpec(
            Enumerable.Range(0, 6).Select(column => new GraphNode($"n{column}", $"N{column}", null, column)).ToList(),
            []);

        var placed = GraphLayout.Place(spec);

        var boxes = placed.Nodes.OrderBy(node => node.X).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(placed.Width, Is.GreaterThan(880), "the drawing widens");
            for (int at = 1; at < boxes.Count; at++)
                Assert.That(boxes[at].X - (boxes[at - 1].X + boxes[at - 1].W), Is.GreaterThanOrEqualTo(60),
                    "room between columns");
        });
    }

    /// <summary>
    /// A word fits on its flat piece (Louis's screenshot of the Turn diagram: "starts" ran onto the host's box): four
    /// columns on a narrow width stand apart far enough for the word, with room either side, and the bend after it.
    /// </summary>
    [Test]
    public void TheColumns_StandApartForTheWidestWord()
    {
        var spec = new GraphSpec(
            Enumerable.Range(0, 4).Select(column => new GraphNode($"n{column}", $"N{column}", null, column)).ToList(),
            [new GraphEdge("n0", "n1", "starts"), new GraphEdge("n1", "n2", "a turn"), new GraphEdge("n2", "n3")]);

        var placed = GraphLayout.Place(spec);

        var word = placed.Edges.Single(edge => edge.Edge.Word == "a turn");
        var from = placed.Nodes.Single(node => node.Node.Id == "n1");
        var to = placed.Nodes.Single(node => node.Node.Id == "n2");
        Assert.Multiple(() =>
        {
            Assert.That(to.X - (from.X + from.W), Is.GreaterThanOrEqualTo(8 * 6 + 16), "the word and its margins");
            Assert.That(word.WordX - 4 * 6, Is.GreaterThanOrEqualTo(from.X + from.W + 8), "clear of the left box");
            Assert.That(word.WordX + 4 * 6, Is.LessThanOrEqualTo(to.X - 8), "clear of the right box");
        });
    }

    /// <summary>
    /// A gap is as wide as the words that cross it, not the widest word of the drawing (Louis's screenshot of the web
    /// diagram: "just take these empty spaces out"): a long word between two columns leaves the next gap narrow.
    /// </summary>
    [Test]
    public void AGap_FitsOnlyItsOwnWords()
    {
        var spec = new GraphSpec(
            Enumerable.Range(0, 4).Select(column => new GraphNode($"n{column}", $"N{column}", null, column)).ToList(),
            [
                new GraphEdge("n0", "n1", "opens"),
                new GraphEdge("n1", "n2", "a message, the page"),
                new GraphEdge("n2", "n3", "a turn")
            ]);

        var placed = GraphLayout.Place(spec);

        var boxes = placed.Nodes.OrderBy(node => node.X).ToList();
        double Gap(int left) => boxes[left + 1].X - (boxes[left].X + boxes[left].W);
        Assert.Multiple(() =>
        {
            Assert.That(Gap(1), Is.GreaterThanOrEqualTo(8 * 19 + 16), "the long word has its room");
            Assert.That(Gap(1), Is.LessThan(2 * (8 * 19 + 16)), "but no empty half after the bend");
            Assert.That(Gap(2), Is.GreaterThanOrEqualTo(8 * 6 + 16), "the short word has its room");
            Assert.That(Gap(2), Is.LessThan(Gap(1)), "and no more");
        });
    }

    /// <summary>
    /// Two words out of one box don't share a line: the second word moves to the piece by its own target.
    /// </summary>
    [Test]
    public void TwoWordsOutOfOneBox_Part()
    {
        var spec = new GraphSpec(
            [new GraphNode("a", "A", null, 0), new GraphNode("x", "X", null, 1), new GraphNode("y", "Y", null, 1)],
            [new GraphEdge("a", "x", "through"), new GraphEdge("a", "y", "served")]);

        var placed = GraphLayout.Place(spec);

        var words = placed.Edges.Select(edge => (edge.WordX, edge.WordY)).Distinct().ToList();
        Assert.That(words, Has.Count.EqualTo(2));
    }

    /// <summary>
    /// Every box sits on one row grid (Louis's screenshots of the Turn and Packs diagrams: two lines a few pixels
    /// apart jogged, "this must not happen, our framework must cater for this"): a row is as tall as the tallest box,
    /// a shorter column is centred on the grid, so any two middles differ by nothing or by half a row at least, and a
    /// shorter box stands in the middle of its row.
    /// </summary>
    [Test]
    public void TheBoxes_SitOnARowGrid()
    {
        var spec = new GraphSpec(
            [
                new GraphNode("a", "A", null, 0), new GraphNode("b", "B", null, 0),
                new GraphNode("c", "C", null, 1), new GraphNode("d", "D", "two lines of note here", 1),
                new GraphNode("e", "E", null, 1)
            ],
            [new GraphEdge("a", "c"), new GraphEdge("b", "e")]);

        var placed = GraphLayout.Place(spec);

        var middles = placed.Nodes.Select(node => node.Y + node.H / 2).Distinct().OrderBy(y => y).ToList();
        var d = placed.Nodes.Single(node => node.Node.Id == "d");
        var c = placed.Nodes.Single(node => node.Node.Id == "c");
        Assert.Multiple(() =>
        {
            for (int at = 1; at < middles.Count; at++)
                Assert.That(middles[at] - middles[at - 1], Is.GreaterThanOrEqualTo((d.H + 18) / 2),
                    "half a row apart at least");
            Assert.That(c.Y + c.H / 2, Is.EqualTo(d.Y + d.H / 2 - (d.H + 18)),
                "one row apart, the short box in the middle of its row");
        });
    }

    /// <summary>
    /// An arrow inside a column (Louis's Turn diagram: "no lines, what are they?", the pipeline a turn runs down)
    /// drops straight from the box's bottom middle to the next box's top middle; its word sits beside the drop.
    /// </summary>
    [Test]
    public void AnArrowDownAColumn_DropsStraight()
    {
        var spec = new GraphSpec(
            [new GraphNode("a", "A", null, 0), new GraphNode("b", "B", null, 0), new GraphNode("c", "C", null, 0)],
            [new GraphEdge("a", "b", "then"), new GraphEdge("b", "c")]);

        var placed = GraphLayout.Place(spec);

        var a = placed.Nodes.Single(node => node.Node.Id == "a");
        var b = placed.Nodes.Single(node => node.Node.Id == "b");
        var drop = placed.Edges.Single(edge => edge.Edge.From == "a");
        Assert.Multiple(() =>
        {
            Assert.That(drop.Path,
                Is.EqualTo($"M {a.X + a.W / 2:0.#} {a.Y + a.H:0.#} L {b.X + b.W / 2:0.#} {b.Y:0.#}"));
            Assert.That(drop.WordX, Is.GreaterThan(a.X + a.W / 2), "the word stands beside the drop");
            Assert.That(drop.WordY, Is.GreaterThan(a.Y + a.H).And.LessThan(b.Y), "in the gap");
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

        var placed = GraphLayout.Place(spec);

        var box = placed.Nodes.Single();
        Assert.That(box.X + box.W / 2, Is.EqualTo(440));
    }
}
