using LouisAgent.Diagrams.Engine.Flow;
using LouisAgent.Diagrams.Flow.Spec;
using LouisAgent.Diagrams.Tests.Flow.Arrows;

namespace LouisAgent.Diagrams.Tests.Flow;

/// <summary>
/// An arrow that passes a column is led through it (CL60.2, F134.12): it runs on the line it arrives on where
/// that is clear, and takes the shortest way through what is clear where no one line lets it through.
/// </summary>
public class LongLinesTests
{
    /// <summary>A diagram of boxes with no columns, and its arrows.</summary>
    private static GraphSpec Spec(string[] boxes, params (string From, string To)[] arrows) =>
        new([.. boxes.Select(id => new GraphNode(id, id))],
            [.. arrows.Select(arrow => new GraphEdge(arrow.From, arrow.To))]);

    /// <summary>
    /// The boxes and arrows of a real picture, F36's "MCP servers as packs", where Louis found two long lines
    /// that wandered: a start-up path down the left with three decisions, a box in the middle, and two arrows
    /// from the top row that both arrive at the catalogue far below.
    /// </summary>
    private static GraphSpec Packs() => Spec(
        [
            "named", "settings", "approved", "unavailable", "connect", "dispose", "drift", "names", "pin", "guard",
            "catalogue", "sessions", "changed", "refresh"
        ],
        ("named", "settings"), ("settings", "approved"), ("approved", "unavailable"), ("approved", "connect"),
        ("connect", "dispose"), ("dispose", "unavailable"), ("connect", "drift"), ("drift", "dispose"),
        ("drift", "names"), ("names", "pin"), ("pin", "dispose"), ("pin", "guard"), ("guard", "catalogue"),
        ("catalogue", "sessions"), ("changed", "catalogue"), ("refresh", "catalogue"));

    /// <summary>How often the line from one box to another turns.</summary>
    private static int Turns(GraphSpec spec, string from, string to) =>
        Ways.Turns(GraphLayout.Place(spec).Edges.Single(edge => edge.Edge.From == from && edge.Edge.To == to).Way);

    /// <summary>
    /// A long line whose arriving line is clear runs on it, though another long line stood there when it was
    /// looked at and has since moved to its own (F134.12, Louis: "can we fix this here"). It turns once out of
    /// its box's line and once into the box, and no more.
    /// </summary>
    [Test]
    public void ALongLine_RunsOnTheLineItArrivesOn_ThoughAnotherStoodThereFirst() =>
        Assert.That(Turns(Packs(), "changed", "catalogue"), Is.LessThanOrEqualTo(2));

    /// <summary>
    /// A long line that no single line lets through takes the shortest way through what is clear, row by row
    /// (F134.12, Louis: "why did it take a longer route?"). It does not swing out past its target and back.
    /// </summary>
    [Test]
    public void ALongLineNoOneLineLetsThrough_TakesTheShortestWay()
    {
        var placed = GraphLayout.Place(Packs());
        var from = placed.Nodes.Single(node => node.Node.Id == "approved");
        var to = placed.Nodes.Single(node => node.Node.Id == "unavailable");
        var way = placed.Edges.Single(edge => edge.Edge.From == "approved" && edge.Edge.To == "unavailable").Way;

        // Every height the line passes, in a picture that runs right: none above the higher of its two boxes or
        // under the lower, a box's own height allowed for.
        var heights = Ways.Ys(way);
        double top = Math.Min(from.Box.Top, to.Box.Top), bottom = Math.Max(from.Box.Bottom, to.Box.Bottom);

        Assert.Multiple(() =>
        {
            Assert.That(heights.Min(), Is.GreaterThanOrEqualTo(top - 1), "it does not swing out above");
            Assert.That(heights.Max(), Is.LessThanOrEqualTo(bottom + 1), "nor under");
            Assert.That(Ways.Turns(way), Is.LessThanOrEqualTo(4));
        });
    }
}
