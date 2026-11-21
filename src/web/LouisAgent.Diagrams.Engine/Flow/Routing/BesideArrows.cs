using LouisAgent.Diagrams.Engine.Flow.Placed;
using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Flow.Spec;

namespace LouisAgent.Diagrams.Engine.Flow.Routing;

/// <summary>
/// The arrows back to the column right beside their source, on the same row and with no word (F164.1, Louis: "that
/// line must be straight the other one can bend cause of the label"): nothing stands between the two boxes, so each
/// runs straight across, where any other arrow back takes its lane under the drawing.
/// </summary>
/// <remarks>
/// A part of <see cref="EdgeRouter"/> of its own, as <see cref="ApartArrows"/> and <see cref="Detours"/> are: the
/// router sorts the arrows and asks this which go straight back, so the rule is in one place.
/// </remarks>
internal static class BesideArrows
{
    /// <summary>How near two boxes' middles stand to count as one row: the row grid puts them level, so within a pixel.</summary>
    private const double SameRow = 1;

    /// <summary>Whether an arrow goes back to the column right beside its source, on the same row, with no word.</summary>
    public static bool Beside(GraphEdge edge, IReadOnlyDictionary<string, PlacedNode> byId)
    {
        if (edge.From == edge.To || edge.Word is not null) return false;
        var from = byId[edge.From];
        var to = byId[edge.To];
        return to.Node.Column == from.Node.Column - 1 && Math.Abs(to.Box.Middle.Y - from.Box.Middle.Y) < SameRow;
    }

    /// <summary>One level line from the source's left side to the target's right, at their shared middle.</summary>
    public static Plan Across(GraphEdge edge, IBox from, IBox to, IGeometry geometry)
    {
        double y = from.Middle.Y;
        return new Plan(edge, [geometry.Point(from.Left, y), geometry.Point(to.Right, y)],
            geometry.Point((from.Left + to.Right) / 2, y));
    }
}
