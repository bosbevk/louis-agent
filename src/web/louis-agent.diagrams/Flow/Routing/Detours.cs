using LouisAgent.Diagrams.Flow.Placed;

namespace LouisAgent.Diagrams.Flow.Routing;

/// <summary>
/// The way round for an arrow whose route would cross a box it doesn't join (CL60.2): out into the gap beside its
/// source, along a lane of its own under the drawing, and into its target, so every piece runs where no box stands.
/// </summary>
internal static class Detours
{
    /// <summary>Whether a straight piece of the way passes through a box the arrow doesn't join, a pixel in.</summary>
    public static bool Crosses(Plan plan, IReadOnlyDictionary<string, PlacedNode> byId) =>
        byId.Values.Where(box => box.Node.Id != plan.Edge.From && box.Node.Id != plan.Edge.To
                                                               && !Ranks.IsWaypoint(box.Node.Id)).Any(box =>
            plan.Points.Zip(plan.Points.Skip(1)).Any(piece =>
                Math.Min(piece.First.X, piece.Second.X) < box.X + box.W - 1
                && Math.Max(piece.First.X, piece.Second.X) > box.X + 1
                && Math.Min(piece.First.Y, piece.Second.Y) < box.Y + box.H - 1
                && Math.Max(piece.First.Y, piece.Second.Y) > box.Y + 1));

    /// <summary>
    /// The way round: out of the source's right side into the gap beside it, down to a lane under the drawing, along
    /// it to the gap by the target, and up into the target's near side; inside one column, down the gap beside it.
    /// Every piece runs in a gap or under the boxes, so it crosses none. Its word sits on the lane.
    /// </summary>
    public static Plan Detour(Plan plan, IReadOnlyDictionary<string, PlacedNode> byId, double lane)
    {
        var (from, to) = (byId[plan.Edge.From], byId[plan.Edge.To]);
        double x1 = from.X + from.W, y1 = from.Y + from.H / 2, y2 = to.Y + to.H / 2, out1 = x1 + EdgeRouter.Step;
        if (to.X == from.X)
            return plan with
            {
                Points = [(x1, y1), (out1, y1), (out1, y2), (to.X + to.W, y2)],
                WordX = out1 + 8 + 4 * (plan.Edge.Word?.Length ?? 0),
                WordY = (y1 + y2) / 2 + 4
            };

        // Back to an earlier column, it comes up into the target's bottom middle, away from the side its own arrows
        // leave by, unless a box stands under the target in its column.
        double entry = to.X + to.W / 2;
        bool under = byId.Values.Any(box => box != to && box.X < entry && box.X + box.W > entry && box.Y > to.Y);
        if (to.X < from.X && !under)
            return plan with
            {
                Points = [(x1, y1), (out1, y1), (out1, lane), (entry, lane), (entry, to.Y + to.H)],
                WordX = (out1 + entry) / 2,
                WordY = lane + 4
            };

        bool ahead = to.X > from.X;
        double x2 = ahead ? to.X : to.X + to.W, in2 = ahead ? x2 - EdgeRouter.Step : x2 + EdgeRouter.Step;
        return plan with
        {
            Points = [(x1, y1), (out1, y1), (out1, lane), (in2, lane), (in2, y2), (x2, y2)],
            WordX = (out1 + in2) / 2,
            WordY = lane + 4
        };
    }
}
