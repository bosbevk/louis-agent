using LouisAgent.Diagrams.Engine.Flow;
using LouisAgent.Diagrams.Engine.Flow.Placed;
using LouisAgent.Diagrams.Flow.Spec;

namespace LouisAgent.Diagrams.Tests.Flow.Routing;

/// <summary>
/// An arrow back to the column right beside it, on the same row and with no word, runs straight across between the
/// two boxes (F164.1, Louis: "that line must be straight the other one can bend cause of the label"); an arrow back
/// with a word, or further back, still takes its lane under the drawing.
/// </summary>
public class BesideBackTests
{
    /// <summary>A path that turns: two along the top, then two under them running back, the last arrow worded.</summary>
    private static GraphSpec Turning() => new(
        [
            new GraphNode("a", "A", null, 0), new GraphNode("b", "B", null, 1),
            new GraphNode("c", "C", null, 1), new GraphNode("d", "D", null, 0)
        ],
        [new GraphEdge("a", "b"), new GraphEdge("b", "c"), new GraphEdge("c", "d", "a word")]);

    /// <summary>The arrow between two boxes, as the layout routed it.</summary>
    private static PlacedEdge Arrow(PlacedGraph placed, string from, string to) =>
        placed.Edges.Single(edge => edge.Edge.From == from && edge.Edge.To == to);

    /// <summary>
    /// Back beside it with no word, the arrow is one level line from the source's left side to the target's right.
    /// </summary>
    [Test]
    public void AnArrowBackBesideIt_RunsStraightAcross()
    {
        var spec = Turning() with
        {
            Nodes =
            [
                new GraphNode("a", "A", null, 0), new GraphNode("b", "B", null, 1),
                new GraphNode("c", "C", null, 1), new GraphNode("d", "D", null, 0)
            ],
            Edges = [new GraphEdge("a", "b"), new GraphEdge("b", "c"), new GraphEdge("c", "d")]
        };
        var placed = GraphLayout.Place(spec);
        var from = placed.Nodes.Single(node => node.Node.Id == "c").Box;
        var to = placed.Nodes.Single(node => node.Node.Id == "d").Box;

        var points = Arrow(placed, "c", "d").Way.Pieces;

        Assert.Multiple(() =>
        {
            Assert.That(points, Has.Count.EqualTo(2), "one straight piece");
            Assert.That(points[0].X, Is.EqualTo(from.Left));
            Assert.That(points[^1].X, Is.EqualTo(to.Right));
            Assert.That(points.Select(point => point.Y).Distinct().Count(), Is.EqualTo(1), "level");
        });
    }

    /// <summary>With a word on it, the arrow back still bends under the drawing, where its word has room.</summary>
    [Test]
    public void AnArrowBackWithAWord_StillTakesItsLane()
    {
        var placed = GraphLayout.Place(Turning());

        Assert.That(Arrow(placed, "c", "d").Way.Pieces, Has.Count.GreaterThan(2));
    }
}
