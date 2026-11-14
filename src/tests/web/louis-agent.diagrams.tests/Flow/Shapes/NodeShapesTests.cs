using System.Globalization;
using LouisAgent.Diagrams.Flow;
using LouisAgent.Diagrams.Flow.Shapes;
using LouisAgent.Diagrams.Flow.Spec;

namespace LouisAgent.Diagrams.Tests.Flow.Shapes;

/// <summary>
/// A box may have a shape where the shape means something (F134.8): a diamond for a decision, a slanted box for
/// data, a pill for a start or an end. A shape is as big as its words need inside it, and an arrow ends on its
/// outline.
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
    /// A diamond is big enough for its words: at the height of each line, the line fits between the two slanted
    /// sides.
    /// </summary>
    [Test]
    public void ADiamond_HoldsItsWords()
    {
        var (w, h) = NodeShapes.Room(Question, 170, 84);
        string[] lines = Question.Label.Split('\n');
        double top = h / 2 - FlowText.LabelStep * lines.Length / 2;

        Assert.Multiple(() =>
        {
            for (int at = 0; at < lines.Length; at++)
            {
                // The edge of the line that is further from the middle, where the diamond is narrower.
                double edge = top + FlowText.LabelStep * (at < lines.Length / 2.0 ? at : at + 1);
                double room = w - 2 * NodeShapes.Inset(NodeShape.Decision, ShapeSide.Left, w, h, edge);
                Assert.That(room, Is.GreaterThanOrEqualTo(lines[at].Length * GraphLayout.LabelCharWidth),
                    $"line {at + 1} fits");
            }
        });
    }

    /// <summary>A diamond's outline is at the box's side only at the tip, and half the box in at its top.</summary>
    [Test]
    public void ADiamondsSide_SlantsFromItsTip()
    {
        Assert.Multiple(() =>
        {
            Assert.That(NodeShapes.Inset(NodeShape.Decision, ShapeSide.Left, 200, 100, 50), Is.Zero, "at the tip");
            Assert.That(NodeShapes.Inset(NodeShape.Decision, ShapeSide.Left, 200, 100, 25), Is.EqualTo(50));
            Assert.That(NodeShapes.Inset(NodeShape.Decision, ShapeSide.Top, 200, 100, 100), Is.Zero, "at the top tip");
            Assert.That(NodeShapes.Inset(NodeShape.Decision, ShapeSide.Bottom, 200, 100, 50), Is.EqualTo(25));
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
    /// Two arrows into a diamond arrive off its tip, so each ends on the slanted side, not at the edge of the box
    /// the diamond is drawn in.
    /// </summary>
    [TestCase(FlowDirection.Right)]
    [TestCase(FlowDirection.Down)]
    public void AnArrowIntoADiamond_EndsOnItsOutline(FlowDirection direction)
    {
        var spec = new GraphSpec(
            [new GraphNode("a", "A"), new GraphNode("b", "B"), Question],
            [new GraphEdge("a", "q"), new GraphEdge("b", "q")], direction);

        var placed = GraphLayout.Place(spec, rules: FlowRules.Straight);
        var diamond = placed.Nodes.Single(node => node.Node.Id == "q");

        Assert.Multiple(() =>
        {
            foreach (var edge in placed.Edges)
            {
                var (x, y) = End(edge.Path);
                // On a diamond, how far a point is from the middle each way, as parts of the half sizes, adds up
                // to one.
                double across = Math.Abs(x - (diamond.X + diamond.W / 2)) / (diamond.W / 2);
                double down = Math.Abs(y - (diamond.Y + diamond.H / 2)) / (diamond.H / 2);
                Assert.That(across + down, Is.EqualTo(1).Within(0.01), $"{edge.Edge.From} ends on the outline");
            }
        });
    }
}
