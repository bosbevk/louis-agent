using System.Globalization;
using LouisAgent.Web.Graphs.Placed;

namespace LouisAgent.Web.Graphs;

/// <summary>
/// Draws every arrow as a connector (FX85, Louis: "lines may not cross, have connectors on lines so they still bend
/// but make direct lines"): straight pieces that turn at right angles, each bend rounded a little. Forward, an arrow
/// leaves the box's right side, runs to the one track between the columns, up or down it and into the target's left
/// side; every arrow between two columns bends there, so they join instead of crossing. Back, it leaves the right side low,
/// drops to a lane under the drawing and climbs into the target's bottom. To its own box, it loops out of the right side.
/// </summary>
/// <remarks>
/// Orthogonal routing, the drawing style of circuit and org charts. Chosen over curves because curves between a tall
/// column and a short one crossed each other and a reader lost which box fed which; on a shared track a crossing
/// becomes a junction, and a back arrow's own track, lane and entry are nested so two of them never cross either.
/// </remarks>
internal static class EdgeRouter
{
    /// <summary>The round at a bend, and the least a piece may be before a bend is drawn sharp.</summary>
    private const double Bend = 8;
    /// <summary>How far under the drawing the first lane runs, and the step between two tracks, lanes or entries.</summary>
    private const double BackLane = 30, Step = 18;
    /// <summary>How far out of a box's right side an arrow to itself reaches.</summary>
    private const double SelfReach = 40;
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    /// <summary>
    /// The room two columns need between them so the widest word sits on a flat piece with space either side (Louis:
    /// "never overlap"): the longer of an arrow's two flat pieces is at least half the gap, so the gap is twice the
    /// word and its margins.
    /// </summary>
    public static double GapFor(IReadOnlyList<GraphEdge> edges) =>
        edges.Count == 0 ? 0 : 2 * (8 * edges.Max(edge => edge.Word?.Length ?? 0) + 16);

    /// <summary>Routes the known arrows; the height and width grow for the lanes and loops that run outside the boxes.</summary>
    public static (List<PlacedEdge> Edges, double Height, double Width) Route(
        IReadOnlyList<GraphEdge> edges, IReadOnlyDictionary<string, PlacedNode> byId, double height, double width)
    {
        var self = edges.Where(edge => edge.From == edge.To).ToList();
        var back = edges.Where(edge => edge.From != edge.To && byId[edge.To].Node.Column < byId[edge.From].Node.Column).ToList();
        var forward = edges.Except(self).Except(back).ToList();

        var placed = new List<PlacedEdge>();
        placed.AddRange(Forward(forward, byId));
        if (back.Count > 0) placed.AddRange(Back(back, byId, height, self));
        placed.AddRange(self.Select(edge => SelfLoop(edge, byId[edge.From])));

        // Room for the lanes under the drawing, and for a track or a loop past the last column.
        if (back.Count > 0) height += BackLane + Step * (back.Select(edge => edge.To).Distinct().Count() - 1) + 8;
        double reach = placed.Count == 0 ? 0 : placed.Max(edge => Math.Max(edge.WordX + 4 * (edge.Edge.Word?.Length ?? 0), edge.Right));
        return (placed, height, Math.Max(width, reach + 4));
    }

    /// <summary>
    /// The forward arrows: every arrow between two columns bends at the same place, one track in the middle of the gap
    /// (Louis: "conformity", "alignment is key"), leaves its box's middle and arrives at its target's middle, so a box
    /// has one line out that branches at the track and the arrows into a box join there. A word sits on the piece by
    /// its source, or by its target when the source has more than one word out, so two words never share a line.
    /// </summary>
    private static IEnumerable<PlacedEdge> Forward(List<GraphEdge> forward, IReadOnlyDictionary<string, PlacedNode> byId)
    {
        var wordsOut = forward.Where(edge => edge.Word is not null).GroupBy(edge => edge.From).ToDictionary(group => group.Key, group => group.Count());
        foreach (var edge in forward)
        {
            var from = byId[edge.From];
            var to = byId[edge.To];
            double x1 = from.X + from.W, y1 = from.Y + from.H / 2;
            double x2 = to.X, y2 = to.Y + to.H / 2;
            double track = (x1 + x2) / 2;
            bool bySource = wordsOut.GetValueOrDefault(edge.From) <= 1;
            yield return Connector(edge, [(x1, y1), (track, y1), (track, y2), (x2, y2)], bySource ? (x1 + track) / 2 : (track + x2) / 2, bySource ? y1 : y2);
        }
    }

    /// <summary>
    /// The arrows back to earlier columns: all the arrows into one box share one track past the last column, one lane
    /// under the drawing and one entry in the box's bottom middle. A second target's set is nested outside the first,
    /// so two sets never cross, and the tracks all start past any loop to itself and its word.
    /// </summary>
    private static IEnumerable<PlacedEdge> Back(List<GraphEdge> back, IReadOnlyDictionary<string, PlacedNode> byId, double height, List<GraphEdge> self)
    {
        var targets = back.Select(edge => edge.To).Distinct().ToList();
        double pastLoops = self.Count == 0 ? 0 : SelfReach + Step + 8 * self.Max(loop => loop.Word?.Length ?? 0);
        double rightEdge = back.Max(edge => byId[edge.From].X + byId[edge.From].W);
        foreach (var edge in back)
        {
            var from = byId[edge.From];
            var to = byId[edge.To];
            int at = targets.IndexOf(edge.To);
            double x1 = from.X + from.W, y1 = from.Y + from.H * 0.8;
            double track = rightEdge + pastLoops + Step + Step * at, lane = height + BackLane + Step * at;
            double entry = to.X + to.W / 2, y2 = to.Y + to.H;
            yield return Connector(edge, [(x1, y1), (track, y1), (track, lane), (entry, lane), (entry, y2)], (track + entry) / 2, lane + 4);
        }
    }

    /// <summary>An arrow from a box to itself: out of its right side near the top, round and back in near the bottom, its word beside it.</summary>
    private static PlacedEdge SelfLoop(GraphEdge edge, PlacedNode box)
    {
        double x = box.X + box.W, y1 = box.Y + box.H * 0.3, y2 = box.Y + box.H * 0.7, reach = x + SelfReach;
        return Connector(edge, [(x, y1), (reach, y1), (reach, y2), (x, y2)], reach + 8 + 4 * (edge.Word?.Length ?? 0), box.Y + box.H / 2 + 4);
    }

    /// <summary>
    /// The path through the points, each bend rounded by a quarter-curve that starts and ends <see cref="Bend"/> from
    /// the corner, or less when a piece is shorter; the length is the pieces' sum, close enough for the stroke that draws it.
    /// </summary>
    private static PlacedEdge Connector(GraphEdge edge, (double X, double Y)[] points, double wordX, double wordY)
    {
        var path = new System.Text.StringBuilder($"M {N(points[0].X)} {N(points[0].Y)}");
        double length = 0;
        for (int at = 1; at < points.Length; at++)
        {
            var (fromX, fromY) = points[at - 1];
            var (toX, toY) = points[at];
            length += Math.Abs(toX - fromX) + Math.Abs(toY - fromY);
            if (at == points.Length - 1)
            {
                path.Append($" L {N(toX)} {N(toY)}");
                continue;
            }

            // Stop short of the corner, then curve through it towards the next point.
            var (nextX, nextY) = points[at + 1];
            double round = Math.Min(Bend, Math.Min(Math.Abs(toX - fromX) + Math.Abs(toY - fromY), Math.Abs(nextX - toX) + Math.Abs(nextY - toY)) / 2);
            if (round < 1) continue;
            var (inX, inY) = (toX - Math.Sign(toX - fromX) * round, toY - Math.Sign(toY - fromY) * round);
            var (outX, outY) = (toX + Math.Sign(nextX - toX) * round, toY + Math.Sign(nextY - toY) * round);
            path.Append($" L {N(inX)} {N(inY)} Q {N(toX)} {N(toY)} {N(outX)} {N(outY)}");
        }

        return new PlacedEdge(edge, path.ToString(), length, wordX, wordY, points.Max(point => point.X));
    }

    private static string N(double value) => value.ToString("0.#", Invariant);
}
