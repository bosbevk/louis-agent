using System.Globalization;
using LouisAgent.Diagrams.Flow.Placed;
using LouisAgent.Diagrams.Flow.Spec;

namespace LouisAgent.Diagrams.Flow.Routing;

/// <summary>
/// Draws every arrow as a connector (FX85, Louis: "lines may not cross, have connectors on lines so they still bend
/// but make direct lines"): straight pieces that turn at right angles, each bend rounded a little. Forward, an arrow
/// leaves the box's right side, runs to the one track between the columns, up or down it and into the target's left
/// side; every arrow between two columns bends there, so they join instead of crossing. Back, it leaves the right side
/// low, drops to a lane under the drawing and climbs into the target's bottom. Inside a column, it drops straight from
/// one box to the next. To its own box, it loops out of the right side.
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

    /// <summary>How far under the drawing the first lane runs.</summary>
    private const double BackLane = 30;

    /// <summary>The step between two tracks, lanes or entries.</summary>
    internal const double Step = 18;

    /// <summary>How far out of a box's right side an arrow to itself reaches.</summary>
    private const double SelfReach = 40;

    /// <summary>The flat piece of an arrow that carries no word: room for the bend and the arrowhead.</summary>
    private const double Stub = 24;

    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    /// <summary>
    /// The room two columns need between them so the widest word of the arrows crossing it sits on a flat piece with
    /// space either side (Louis: "never overlap"): the longer of an arrow's two flat pieces is at least half the
    /// gap, so the gap is twice the word and its margins.
    /// </summary>
    public static double GapFor(IReadOnlyList<GraphEdge> edges, bool turned = false) =>
        edges.Count == 0 ? 0 : 2 * edges.Max(edge => edge.Word is { } word ? RoomOf(word, turned) : 0);

    /// <summary>
    /// The room either side of the bend for the arrows between two neighbouring columns (Louis: "this one can still
    /// move closer"): the source's side as long as the widest word that sits by its source, the target's side as long
    /// as the widest word by its target, and a side with no word only a stub for the bend and the arrowhead.
    /// </summary>
    public static (double Source, double Target) RoomsFor(
        IReadOnlyList<GraphEdge> between,
        IReadOnlyList<GraphEdge> forward,
        bool turned = false)
    {
        var wordsOut = WordsOut(forward);

        double Room(bool bySource) => between
            .Where(edge => edge.Word is not null && wordsOut.GetValueOrDefault(edge.From) <= 1 == bySource)
            .Select(edge => RoomOf(edge.Word!, turned)).DefaultIfEmpty(Stub).Max();

        return (Room(true), Room(false));
    }

    /// <summary>
    /// The room a word takes along its arrow: its length, lying on a flat piece; laid out turned, its lines' height,
    /// since once turned back it stands beside an upright line (CL60.2).
    /// </summary>
    private static double RoomOf(string word, bool turned) =>
        turned ? FlowText.WordStep * FlowText.WordLines(word).Count + 22 : 8 * word.Length + 16;

    /// <summary>
    /// Where a word stands on its piece: in the middle, lying on it; laid out turned, at the top of its room, so its
    /// lines stand under the piece's start and clear of the bend.
    /// </summary>
    private static double WordAlong(double start, double end, bool turned) => turned ? start + 18 : (start + end) / 2;

    /// <summary>
    /// How many arrows with a word leave each box; a box with two or more puts its words by their targets.
    /// </summary>
    private static Dictionary<string, int> WordsOut(IEnumerable<GraphEdge> forward) =>
        forward.Where(edge => edge.Word is not null).GroupBy(edge => edge.From)
            .ToDictionary(group => group.Key, group => group.Count());

    /// <summary>
    /// Routes the known arrows; the height and width grow for the lanes and loops that run outside the boxes.
    /// </summary>
    public static (List<PlacedEdge> Edges, double Height, double Width) Route(
        IReadOnlyList<GraphEdge> edges,
        IReadOnlyDictionary<string, PlacedNode> byId,
        double height,
        double width,
        bool turned = false)
    {
        var self = edges.Where(edge => edge.From == edge.To).ToList();
        var back = edges.Where(edge => edge.From != edge.To && byId[edge.To].Node.Column < byId[edge.From].Node.Column)
            .ToList();
        var down = edges.Where(edge => edge.From != edge.To && byId[edge.To].Node.Column == byId[edge.From].Node.Column)
            .ToList();
        var forward = edges.Except(self).Except(back).Except(down).ToList();

        var plans = new List<Plan>();
        plans.AddRange(Forward(forward, byId, turned));
        plans.AddRange(down.Select(edge => Down(edge, byId[edge.From], byId[edge.To])));
        if (back.Count > 0) plans.AddRange(Back(back, byId, height));
        int backLanes = back.Select(edge => edge.To).Distinct().Count();

        // An arrow whose way would cross a box it doesn't join, as a docs chart's arrow past a rank can (CL60.2),
        // takes a detour: down its gap, along a lane of its own under the drawing, and up the gap by its target.
        double firstFree = height + BackLane + Step * backLanes;
        int detours = 0;
        var placed = plans.Select(plan => Connector(Detours.Crosses(plan, byId)
            ? Detours.Detour(plan, byId, firstFree + Step * detours++)
            : plan)).ToList();
        placed.AddRange(self.Select(edge => Connector(SelfLoop(edge, byId[edge.From]))));

        // Room for the lanes under the drawing, and for a track or a loop past the last column.
        if (back.Count > 0) height += BackLane + Step * (backLanes - 1) + 8;
        if (detours > 0) height = firstFree + Step * (detours - 1) + 8;

        // A word reaches along its piece half its length, or laid out turned its lines' height.
        double Reach(PlacedEdge edge) => edge.Edge.Word is not { } word
            ? 0
            : turned
                ? FlowText.WordStep * FlowText.WordLines(word).Count
                : 4 * word.Length;

        double reach = placed.Count == 0 ? 0 : placed.Max(edge => Math.Max(edge.WordX + Reach(edge), edge.Right));
        return (placed, height, Math.Max(width, reach + 4));
    }

    /// <summary>
    /// The forward arrows: every arrow between two columns bends at the same place, one track in the gap (Louis:
    /// "conformity", "alignment is key"), leaves its box's middle and arrives at its target's middle, so a box has one
    /// line out that branches at the track and the arrows into a box join there. A word sits on the piece by its
    /// source, or by its target when the source has more than one word out, so two words never share a line. Between
    /// neighbouring columns the track stands where <see cref="RoomsFor"/> puts it, the spare room shared either side;
    /// an arrow that skips a column bends in the middle.
    /// </summary>
    private static IEnumerable<Plan> Forward(List<GraphEdge> forward,
        IReadOnlyDictionary<string, PlacedNode> byId, bool turned)
    {
        var wordsOut = WordsOut(forward);
        foreach (var edge in forward)
        {
            var from = byId[edge.From];
            var to = byId[edge.To];
            double x1 = from.X + from.W, y1 = from.Y + from.H / 2;
            double x2 = to.X, y2 = to.Y + to.H / 2;
            var (track, last) = Track(edge, x1, x2, forward, byId, turned);
            bool bySource = wordsOut.GetValueOrDefault(edge.From) <= 1;
            // A word by its target stands past the gap's last track, clear of the other sources' lines.
            yield return new Plan(edge, [(x1, y1), (track, y1), (track, y2), (x2, y2)],
                bySource ? WordAlong(x1, track, turned) : WordAlong(last, x2, turned), bySource ? y1 : y2);
        }
    }

    /// <summary>
    /// Where an arrow bends: between neighbouring columns, after the source's room plus half what the gap has to spare,
    /// the same for every arrow of that gap; across a column between, in the middle.
    /// </summary>
    private static (double Track, double Last) Track(GraphEdge edge, double x1, double x2, List<GraphEdge> forward,
        IReadOnlyDictionary<string, PlacedNode> byId, bool turned)
    {
        bool Between(PlacedNode node) => node.X >= x1 && node.X < x2;
        if (byId.Values.Any(Between)) return ((x1 + x2) / 2, (x1 + x2) / 2);

        // Every arrow from the same column's right edge to the same column's left edge shares this gap.
        var gap = forward.Where(other => byId[other.From].X + byId[other.From].W == x1 && byId[other.To].X == x2)
            .ToList();
        var (words, wordsIn) = RoomsFor(gap, forward, turned);
        var (frameOut, frameIn) = FrameRooms(gap, id => byId[id].Node.Group, turned);
        double source = words + frameOut, target = wordsIn + frameIn;
        // Several sources feeding several boxes each bend on a track of their own, top to bottom (CL60.2), so a
        // reader can follow who feeds whom; the room for those tracks comes off the gap first.
        var sources = TrackOrder(gap, byId);
        double tracks = TracksRoom(gap), room = x2 - x1 - tracks;
        double offset = tracks == 0 ? 0 : Step * sources.IndexOf(edge.From);
        // A gap narrower than its words' rooms, as a chart that runs down keeps it (CL60.2), bends in its middle; half
        // a pixel's slack, since a gap sized to its rooms exactly can come out a hair short in floating point.
        double first = room + 0.5 < source + target ? x1 + room / 2 : x1 + source + (room - source - target) / 2;
        return (first + offset, first + tracks);
    }

    /// <summary>
    /// The order of a gap's tracks, nearest the sources first: the bottom source first, and a source level with
    /// another's target always before that other, so its line out ends before the other's line in starts and the two
    /// never lie on one another (Louis, of the evals chart).
    /// </summary>
    private static List<string> TrackOrder(List<GraphEdge> gap, IReadOnlyDictionary<string, PlacedNode> byId)
    {
        double Middle(string id) => byId[id].Y + byId[id].H / 2;
        var order = gap.Select(edge => edge.From).Distinct().OrderByDescending(Middle).ToList();

        bool Before(string a, string b) => gap.Any(edge => edge.From == b && Math.Abs(Middle(edge.To) - Middle(a)) < 1);
        // A few passes move each source ahead of any it must come before; a cycle of such rules keeps its order.
        for (int pass = 0; pass < order.Count; pass++)
            for (int at = 1; at < order.Count; at++)
                if (Before(order[at], order[at - 1]) && !Before(order[at - 1], order[at]))
                    (order[at - 1], order[at]) = (order[at], order[at - 1]);
        return order;
    }

    /// <summary>
    /// The room a group's frame takes out of a gap an arrow crosses into or out of it (Louis, of the checks chart): on
    /// the source's side the frame's margin when an arrow leaves a group, on the target's side its margin, and its
    /// label once turned back, when one enters a group; so the bend stands outside every frame.
    /// </summary>
    public static (double Source, double Target) FrameRooms(IEnumerable<GraphEdge> between,
        Func<string, string?> groupOf, bool turned)
    {
        var arrows = between.ToList();
        bool leaves = arrows.Any(edge => groupOf(edge.From) is { } group && group != groupOf(edge.To));
        bool enters = arrows.Any(edge => groupOf(edge.To) is { } group && group != groupOf(edge.From));
        return (leaves ? Bands.Pad + 8 : 0, enters ? Bands.Pad + 8 + (turned ? Bands.LabelRoom : 0) : 0);
    }

    /// <summary>
    /// The room a gap's arrows need for tracks of their own: none when one box feeds the rest or all feed one box,
    /// where they join on one track (Louis: "conformity"); otherwise a step for each source after the first.
    /// </summary>
    public static double TracksRoom(IReadOnlyList<GraphEdge> between)
    {
        int sources = between.Select(edge => edge.From).Distinct().Count();
        bool joined = sources <= 1 || between.Select(edge => edge.To).Distinct().Count() == 1;
        return joined ? 0 : Step * (sources - 1);
    }

    /// <summary>
    /// The arrows back to earlier columns: all the arrows into one box share one track past the last column, one lane
    /// under the drawing and one entry in the box's bottom middle. A second target's set is nested outside the first,
    /// so two sets never cross, and the tracks all start past any loop to itself and its word.
    /// </summary>
    private static IEnumerable<Plan> Back(List<GraphEdge> back, IReadOnlyDictionary<string, PlacedNode> byId,
        double height)
    {
        var targets = back.Select(edge => edge.To).Distinct().ToList();
        var sources = back.Select(edge => edge.From).ToHashSet();
        foreach (var edge in back)
        {
            var from = byId[edge.From];
            var to = byId[edge.To];
            int at = targets.IndexOf(edge.To);
            // Straight out of the source's bottom middle to the lane, in a chart that runs down its right side
            // (Louis, of the runner and MCP charts); a box in the way sends it on a detour instead. A box that both
            // sends and receives one sends a little before its middle and receives a little after, so the two
            // never share a stretch.
            double outAt = targets.Contains(edge.From) ? 0.4 : 0.5, inAt = sources.Contains(edge.To) ? 0.6 : 0.5;
            double x1 = from.X + from.W * outAt, y1 = from.Y + from.H, lane = height + BackLane + Step * at;
            double entry = to.X + to.W * inAt, y2 = to.Y + to.H;
            yield return new Plan(edge, [(x1, y1), (x1, lane), (entry, lane), (entry, y2)], (x1 + entry) / 2,
                lane + 4);
        }
    }

    /// <summary>
    /// An arrow inside a column (a pipeline a turn runs down): a straight drop from the box's bottom middle to the next
    /// box's top middle, its word beside it in the gap. Rising, it climbs the same way, from the top to the bottom.
    /// </summary>
    private static Plan Down(GraphEdge edge, PlacedNode from, PlacedNode to)
    {
        bool falls = to.Y >= from.Y;
        double x = from.X + from.W / 2, y1 = falls ? from.Y + from.H : from.Y, y2 = falls ? to.Y : to.Y + to.H;
        return new Plan(edge, [(x, y1), (x, y2)], x + 8 + 4 * (edge.Word?.Length ?? 0), (y1 + y2) / 2 + 4);
    }

    /// <summary>
    /// An arrow from a box to itself: out of its right side near the top, round and back in near the bottom, its word
    /// beside it.
    /// </summary>
    private static Plan SelfLoop(GraphEdge edge, PlacedNode box)
    {
        double x = box.X + box.W, y1 = box.Y + box.H * 0.3, y2 = box.Y + box.H * 0.7, reach = x + SelfReach;
        return new Plan(edge, [(x, y1), (reach, y1), (reach, y2), (x, y2)], reach + 8 + 4 * (edge.Word?.Length ?? 0),
            box.Y + box.H / 2 + 4);
    }

    /// <summary>
    /// The path through the points, each bend rounded by a quarter-curve that starts and ends <see cref="Bend"/> from
    /// the corner, or less when a piece is shorter; the length is the pieces' sum, close enough for the stroke that
    /// draws it.
    /// </summary>
    private static PlacedEdge Connector(Plan plan)
    {
        var (edge, points, wordX, wordY) = plan;
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
            double round = Math.Min(Bend,
                Math.Min(Math.Abs(toX - fromX) + Math.Abs(toY - fromY), Math.Abs(nextX - toX) + Math.Abs(nextY - toY)) /
                2);
            if (round < 1) continue;
            var (inX, inY) = (toX - Math.Sign(toX - fromX) * round, toY - Math.Sign(toY - fromY) * round);
            var (outX, outY) = (toX + Math.Sign(nextX - toX) * round, toY + Math.Sign(nextY - toY) * round);
            path.Append($" L {N(inX)} {N(inY)} Q {N(toX)} {N(toY)} {N(outX)} {N(outY)}");
        }

        return new PlacedEdge(edge, path.ToString(), length, wordX, wordY, points.Max(point => point.X));
    }

    private static string N(double value) => value.ToString("0.#", Invariant);
}
