using LouisAgent.Diagrams.Engine.Flow.Placed;
using LouisAgent.Diagrams.Engine.Geometry;

namespace LouisAgent.Diagrams.Engine.Flow.Routing;

/// <summary>
/// The way round for an arrow whose route would cross a box it doesn't join (CL60.2): out into the gap beside its
/// source, along a lane of its own under the drawing, and into its target, so every piece runs where no box stands.
/// </summary>
internal static class Detours
{
    /// <summary>How far inside a box a line must pass to count as crossing it: touching its edge does not.</summary>
    private const double In = 1;

    /// <summary>Whether a straight piece of the way passes through a box the arrow doesn't join, a pixel in.</summary>
    public static bool Crosses(Plan plan, IReadOnlyDictionary<string, PlacedNode> byId) =>
        byId.Values.Where(node => node.Node.Id != plan.Edge.From && node.Node.Id != plan.Edge.To
                                                                 && !LongLines.IsWaypoint(node.Node.Id))
            .Select(node => node.Box).Any(box =>
                plan.Points.Zip(plan.Points.Skip(1)).Any(piece => box.CrossedBy(piece.First, piece.Second, In)));

    /// <summary>
    /// The way round: out of the source's right side into the gap beside it, down to a lane under the drawing, along
    /// it to the gap by the target, and up into the target's near side; inside one column, down the gap beside it.
    /// Every piece runs in a gap or under the boxes, so it crosses none. Its word sits on the lane.
    /// </summary>
    /// <remarks>Guard clauses, one way round each from the top (ADR 0051).</remarks>
    public static Plan Detour(Plan plan, IReadOnlyDictionary<string, PlacedNode> byId, double lane,
        IGeometry geometry)
    {
        var target = byId[plan.Edge.To];
        IBox from = byId[plan.Edge.From].Box, to = target.Box;
        double x1 = from.Right, y1 = from.Middle.Y, y2 = to.Middle.Y, out1 = x1 + EdgeRouter.Step;
        IPoint At(double x, double y) => geometry.Point(x, y);

        // Inside one column: down the gap beside it, its word beside the upright line.
        if (to.Left == from.Left)
            return plan with
            {
                Points = [At(x1, y1), At(out1, y1), At(out1, y2), At(to.Right, y2)],
                Word = At(EdgeRouter.BesideLine(out1, plan.Edge.Word), (y1 + y2) / 2 + EdgeRouter.WordUnder)
            };

        // Back to an earlier column, it comes up into the target's bottom middle, away from the side its own arrows
        // leave by, unless a box stands under the target in its column.
        double entry = to.Middle.X;
        bool under = byId.Values.Where(node => node != target).Select(node => node.Box)
            .Any(box => box.Left < entry && box.Right > entry && box.Top > to.Top);
        if (to.Left < from.Left && !under)
            return plan with
            {
                Points = [At(x1, y1), At(out1, y1), At(out1, lane), At(entry, lane), At(entry, to.Bottom)],
                Word = At((out1 + entry) / 2, lane + EdgeRouter.WordUnder)
            };

        bool ahead = to.Left > from.Left;
        double x2 = ahead ? to.Left : to.Right, in2 = ahead ? x2 - EdgeRouter.Step : x2 + EdgeRouter.Step;
        return plan with
        {
            Points = [At(x1, y1), At(out1, y1), At(out1, lane), At(in2, lane), At(in2, y2), At(x2, y2)],
            Word = At((out1 + in2) / 2, lane + EdgeRouter.WordUnder)
        };
    }
}
