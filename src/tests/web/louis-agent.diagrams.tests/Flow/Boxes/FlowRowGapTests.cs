using LouisAgent.Diagrams.Engine.Flow;
using LouisAgent.Diagrams.Flow.Spec;

namespace LouisAgent.Diagrams.Tests.Flow.Boxes;

/// <summary>
/// A flow may ask for more room between the boxes stacked in a column (F160.1, Louis: "increase the space between top
/// and bottom a bit"), and one that doesn't is laid out as every flow always was.
/// </summary>
public class FlowRowGapTests
{
    /// <summary>Two boxes stacked in one column, the room between them asked for or not.</summary>
    private static GraphSpec Stacked(double? rowGap = null) => new(
        [new GraphNode("top", "On top", null, 0), new GraphNode("under", "Under it", null, 0)],
        [new GraphEdge("top", "under")], RowGap: rowGap);

    /// <summary>The room between the two boxes, from the bottom of the one to the top of the other.</summary>
    private static double Between(GraphSpec spec)
    {
        var placed = GraphLayout.Place(spec);
        var top = placed.Nodes.Single(node => node.Node.Id == "top").Box;
        var under = placed.Nodes.Single(node => node.Node.Id == "under").Box;
        return under.Top - top.Bottom;
    }

    /// <summary>More room asked for is more room between the rows; none asked for is the usual room.</summary>
    [Test]
    public void AFlowsRowGap_WidensTheRoomBetweenStackedBoxes()
    {
        double usual = Between(Stacked());

        Assert.That(Between(Stacked(rowGap: usual + 40)), Is.GreaterThan(usual));
    }
}
