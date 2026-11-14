using System.Globalization;
using LouisAgent.Diagrams.Flow;
using LouisAgent.Diagrams.Flow.Placed;
using LouisAgent.Diagrams.Flow.Shapes;
using LouisAgent.Diagrams.Flow.Spec;

namespace LouisAgent.Diagrams.Tests.Flow.Shapes;

/// <summary>
/// A box may have a shape where the shape means something (F134.8): a box with pointed ends for a decision
/// (F134.12, a diamond before), a slanted box for data, a pill for a start or an end. A shape is as big as its
/// words need inside it, and an arrow ends on its outline.
/// </summary>
public class NodeShapesTests
{
    /// <summary>A question in three lines, the longest sixteen characters.</summary>
    private static readonly GraphNode Question = new("q", "Commit verified,\nreplay 200,\ntests pass?",
        Shape: NodeShape.Decision);

    /// <summary>The last point of a path, where its arrow arrives.</summary>
    private static (double X, double Y) End(string path)
    {
        var numbers = path.Split(' ', 'L', 'M', 'Q').Where(part => part.Length > 0)
            .Select(part => double.Parse(part, CultureInfo.InvariantCulture)).ToList();
        return (numbers[^2], numbers[^1]);
    }

    /// <summary>A plain box keeps its size, and its outline is its sides: nothing is inside it anywhere.</summary>
    [Test]
    public void APlainBox_KeepsItsSizeAndItsSides()
    {
        Assert.Multiple(() =>
        {
            Assert.That(NodeShapes.Room(new GraphNode("a", "A"), 170, 52), Is.EqualTo((170.0, 52.0)));
            Assert.That(NodeShapes.Inset(NodeShape.Box, ShapeSide.Left, 170, 52, 8), Is.Zero);
        });
    }

    /// <summary>
    /// A decision is a box with pointed ends (F134.12): as tall as a plain box with its words, and wider only by
    /// a point. At the height of each line, the line fits between the two slanted ends.
    /// </summary>
    [Test]
    public void ADecision_IsAsTallAsItsWords_AndHoldsThem()
    {
        var (w, h) = NodeShapes.Room(Question, 170, 84);
        string[] lines = Question.Label.Split('\n');
        double top = h / 2 - FlowText.LabelStep * lines.Length / 2;

        Assert.Multiple(() =>
        {
            Assert.That((w, h), Is.EqualTo((170 + NodeShapes.Point, 84.0)));
            for (int at = 0; at < lines.Length; at++)
            {
                // The edge of the line that is further from the middle, where the ends reach further in.
                double edge = top + FlowText.LabelStep * (at < lines.Length / 2.0 ? at : at + 1);
                double room = w - 2 * NodeShapes.Inset(NodeShape.Decision, ShapeSide.Left, w, h, edge);
                Assert.That(room, Is.GreaterThanOrEqualTo(lines[at].Length * GraphLayout.LabelCharWidth),
                    $"line {at + 1} fits");
            }
        });
    }

    /// <summary>
    /// A decision's end is a point at half its height, and slants in to a corner a point's length from the tip.
    /// Its top and its bottom are flat between the corners.
    /// </summary>
    [Test]
    public void ADecisionsEnd_IsAPoint_AndItsTopIsFlat()
    {
        double point = NodeShapes.Point;

        Assert.Multiple(() =>
        {
            Assert.That(NodeShapes.Inset(NodeShape.Decision, ShapeSide.Left, 200, 100, 50), Is.Zero, "at the tip");
            Assert.That(NodeShapes.Inset(NodeShape.Decision, ShapeSide.Left, 200, 100, 0), Is.EqualTo(point));
            Assert.That(NodeShapes.Inset(NodeShape.Decision, ShapeSide.Right, 200, 100, 75), Is.EqualTo(point / 2));
            Assert.That(NodeShapes.Inset(NodeShape.Decision, ShapeSide.Top, 200, 100, 100), Is.Zero, "the flat top");
            Assert.That(NodeShapes.Inset(NodeShape.Decision, ShapeSide.Top, 200, 100, point), Is.Zero, "a corner");
            Assert.That(NodeShapes.Inset(NodeShape.Decision, ShapeSide.Top, 200, 100, 0), Is.EqualTo(50), "the tip");
            Assert.That(NodeShapes.Inset(NodeShape.Decision, ShapeSide.Bottom, 200, 100, 200 - point / 2),
                Is.EqualTo(25));
        });
    }

    /// <summary>A decision's outline has six corners: a tip each side, and two corners above and under.</summary>
    [Test]
    public void ADecisionsOutline_HasSixCorners()
    {
        string? outline = NodeShapes.PathOf(new PlacedNode(Question, 10, 20, 200, 100));

        Assert.That(outline, Is.EqualTo($"M10 70L{10 + NodeShapes.Point} 20L{210 - NodeShapes.Point} 20L210 70" +
                                        $"L{210 - NodeShapes.Point} 120L{10 + NodeShapes.Point} 120Z"));
    }

    /// <summary>
    /// A small decision, as the key draws its sample, has points no longer than half its height, so its outline
    /// never crosses itself, and an arrow meets that same outline.
    /// </summary>
    [Test]
    public void ASmallDecision_HasShorterPoints()
    {
        string? outline = NodeShapes.PathOf(new PlacedNode(Question, 0, 0, 30, 16));

        Assert.Multiple(() =>
        {
            Assert.That(outline, Is.EqualTo("M0 8L8 0L22 0L30 8L22 16L8 16Z"));
            Assert.That(NodeShapes.Inset(NodeShape.Decision, ShapeSide.Left, 30, 16, 0), Is.EqualTo(8));
            Assert.That(NodeShapes.Inset(NodeShape.Decision, ShapeSide.Top, 30, 16, 8), Is.Zero);
        });
    }

    /// <summary>
    /// A slanted box leans to the right: its left side is in at the top, its right side in at the bottom.
    /// </summary>
    [Test]
    public void ASlantedBox_LeansToTheRight()
    {
        Assert.Multiple(() =>
        {
            Assert.That(NodeShapes.Inset(NodeShape.Data, ShapeSide.Left, 200, 60, 0), Is.EqualTo(NodeShapes.Skew));
            Assert.That(NodeShapes.Inset(NodeShape.Data, ShapeSide.Left, 200, 60, 60), Is.Zero);
            Assert.That(NodeShapes.Inset(NodeShape.Data, ShapeSide.Right, 200, 60, 30),
                Is.EqualTo(NodeShapes.Skew / 2));
            Assert.That(NodeShapes.Inset(NodeShape.Data, ShapeSide.Top, 200, 60, 100), Is.Zero, "the top is flat");
        });
    }

    /// <summary>
    /// A pill's ends are half circles: its side is at the box's side in the middle and curves in from there.
    /// </summary>
    [Test]
    public void APillsEnd_IsRound()
    {
        Assert.Multiple(() =>
        {
            Assert.That(NodeShapes.Inset(NodeShape.Terminal, ShapeSide.Left, 200, 60, 30), Is.Zero, "the middle");
            Assert.That(NodeShapes.Inset(NodeShape.Terminal, ShapeSide.Left, 200, 60, 0), Is.EqualTo(30), "the top");
            Assert.That(NodeShapes.Inset(NodeShape.Terminal, ShapeSide.Top, 200, 60, 100), Is.Zero, "the flat top");
            Assert.That(NodeShapes.Inset(NodeShape.Terminal, ShapeSide.Top, 200, 60, 0), Is.EqualTo(30), "the corner");
        });
    }

    /// <summary>
    /// Two arrows into a decision arrive off its tip, so each ends on its outline, a slanted end or the flat top,
    /// and not at the edge of the box the shape is drawn in.
    /// </summary>
    [TestCase(FlowDirection.Right)]
    [TestCase(FlowDirection.Down)]
    public void AnArrowIntoADecision_EndsOnItsOutline(FlowDirection direction)
    {
        var spec = new GraphSpec(
            [new GraphNode("a", "A"), new GraphNode("b", "B"), Question],
            [new GraphEdge("a", "q"), new GraphEdge("b", "q")], direction);

        var placed = GraphLayout.Place(spec, rules: FlowRules.Straight);
        var shape = placed.Nodes.Single(node => node.Node.Id == "q");

        Assert.Multiple(() =>
        {
            foreach (var edge in placed.Edges)
            {
                var (x, y) = End(edge.Path);
                // How far the point is from the middle: across in pixels, and down as a part of half the height.
                double across = Math.Abs(x - (shape.X + shape.W / 2));
                double down = Math.Abs(y - (shape.Y + shape.H / 2)) / (shape.H / 2);
                // On a slanted end the outline is a point's length further in at the top than at the tip. On the
                // flat top or bottom it is anywhere between the two corners.
                bool onAnEnd = Math.Abs(across - (shape.W / 2 - NodeShapes.Point * down)) < 0.1;
                bool onTheFlat = Math.Abs(down - 1) < 0.01 && across <= shape.W / 2 - NodeShapes.Point + 0.1;
                Assert.That(onAnEnd || onTheFlat, Is.True, $"{edge.Edge.From} ends on the outline, at {x} {y}");
            }
        });
    }
}
