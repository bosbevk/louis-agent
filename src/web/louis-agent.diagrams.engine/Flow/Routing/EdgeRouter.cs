using LouisAgent.Diagrams.Engine.Flow.Arrows;
using LouisAgent.Diagrams.Engine.Flow.Placed;
using LouisAgent.Diagrams.Engine.Flow.Shapes;
using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Words;
using LouisAgent.Diagrams.Flow.Spec;

namespace LouisAgent.Diagrams.Engine.Flow.Routing;

/// <summary>
/// Draws every arrow as a connector (FX85, Louis: "lines may not cross, have connectors on lines so they still bend
/// but make direct lines"): straight pieces that turn at right angles, each bend rounded a little. Forward, an arrow
/// leaves the box's right side, runs to its track between the columns, up or down it and into the target's left
/// side; which track that is, and where on the side it arrives, is <see cref="ApartArrows"/>'s to say. Back, it
/// leaves the right side low, drops to a lane under the drawing and climbs into the target's bottom.
/// Inside a column, it drops straight from one box to the next. To its own box, it loops out of the right side.
/// </summary>
/// <remarks>
/// Orthogonal routing, the drawing style of circuit and org charts. Chosen over curves because curves between a tall
/// column and a short one crossed each other and a reader lost which box fed which; on a shared track a crossing
/// becomes a junction, and a back arrow's own track, lane and entry are nested so two of them never cross either.
/// It asks each box for its edges and its middle, and makes every corner a point of the geometry it was handed.
/// </remarks>
internal static class EdgeRouter
{
    /// <summary>The round at a bend on the site, where no other is asked for.</summary>
    internal const double Bend = 8;

    /// <summary>How far under the drawing the first lane runs.</summary>
    private const double BackLane = 30;

    /// <summary>The step between two tracks, lanes or entries.</summary>
    internal const double Step = 18;

    /// <summary>How far out of a box's right side an arrow to itself reaches.</summary>
    private const double SelfReach = 40;

    /// <summary>The flat piece of an arrow that carries no word: room for the bend and the arrowhead.</summary>
    private const double Stub = 24;

    /// <summary>
    /// A word as the router makes room for it, lying on its arrow: wider than it is drawn, so it never crowds the
    /// bend. And the room it keeps at its two ends together.
    /// </summary>
    private static readonly Face Lying = new(8);

    private const double WordEnds = 16;

    /// <summary>What a word standing beside an upright line adds to its lines' height, laid out turned.</summary>
    private const double TurnedWordRoom = 24;

    /// <summary>
    /// How far a word stands from the upright line it is beside, and how far under the middle of its room a
    /// word's point is put.
    /// </summary>
    private const double WordBeside = 8;

    internal const double WordUnder = 4;

    /// <summary>
    /// The room under the last lane, where the drawing ends, and past the furthest word or loop, at its right.
    /// </summary>
    private const double UnderLanes = 8, PastReach = 4;

    /// <summary>
    /// Where along a box's bottom an arrow back leaves and arrives: its middle, or a little before it to leave and
    /// a little after to arrive, for a box that does both.
    /// </summary>
    private const double Middle = 0.5, Before = 0.4, After = 0.6;

    /// <summary>How far down a box's right side an arrow to itself leaves, and comes back in.</summary>
    private const double LoopOut = 0.3, LoopIn = 0.7;

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
    private static double RoomOf(string word, bool turned) => turned
        ? FlowText.WordStep * FlowText.WordLines(word).Count + FlowText.ChipExtra + TurnedWordRoom
        : Lying.Width(word) + WordEnds;

    /// <summary>
    /// Where a word stands on its piece: in the middle, lying on it; laid out turned, at the top of its room, so its
    /// chip starts a little under the piece's start, clear of the track it hangs from.
    /// </summary>
    private static double WordAlong(double start, double end, bool turned) =>
        turned ? start + FlowText.ChipRise + ChipClear : (start + end) / 2;

    /// <summary>How far under the start of its piece a turned word's chip starts, clear of its track.</summary>
    private const double ChipClear = 4;

    /// <summary>
    /// Where a word stands beside an upright line: past it by a gap, and half the word's own room. An arrow with
    /// no word has its point just past the gap.
    /// </summary>
    internal static double BesideLine(double line, string? word) => line + WordBeside + Lying.Width(word ?? "") / 2;

    /// <summary>
    /// How many arrows with a word leave each box; a box with two or more puts its words by their targets.
    /// </summary>
    private static Dictionary<string, int> WordsOut(IEnumerable<GraphEdge> forward) =>
        forward.Where(edge => edge.Word is not null).GroupBy(edge => edge.From)
            .ToDictionary(group => group.Key, group => group.Count());

    /// <summary>
    /// Routes the known arrows, their corners rounded by <paramref name="bend"/>; the height and width grow for the
    /// lanes and loops that run outside the boxes.
    /// </summary>
    public static (List<PlacedEdge> Edges, double Height, double Width) Route(
        IReadOnlyList<GraphEdge> edges,
        IReadOnlyDictionary<string, PlacedNode> byId,
        double height,
        double width,
        IGeometry geometry,
        bool turned = false,
        double bend = Bend)
    {
        var self = edges.Where(edge => edge.From == edge.To).ToList();
        // Back to the column beside it on the same row, with no word, an arrow runs straight across between the two
        // (Louis: "that line must be straight the other one can bend cause of the label"); every other arrow back
        // takes its lane under the drawing, where a word has room.
        var beside = edges.Where(edge => BesideArrows.Beside(edge, byId)).ToList();
        var back = edges.Where(edge => edge.From != edge.To && byId[edge.To].Node.Column < byId[edge.From].Node.Column)
            .Except(beside).ToList();
        var down = edges.Where(edge => edge.From != edge.To && byId[edge.To].Node.Column == byId[edge.From].Node.Column)
            .ToList();
        var forward = edges.Except(self).Except(back).Except(beside).Except(down).ToList();

        var plans = new List<Plan>();
        plans.AddRange(Forward(forward, byId, turned, geometry));
        plans.AddRange(beside.Select(edge =>
            BesideArrows.Across(edge, byId[edge.From].Box, byId[edge.To].Box, geometry)));
        plans.AddRange(down.Select(edge => Down(edge, byId[edge.From].Box, byId[edge.To].Box, geometry)));
        if (back.Count > 0) plans.AddRange(Back(back, byId, height, turned, geometry));
        int backLanes = back.Select(edge => edge.To).Distinct().Count();

        // An arrow whose way would cross a box it doesn't join, as a docs chart's arrow past a rank can (CL60.2),
        // takes a detour: down its gap, along a lane of its own under the drawing, and up the gap by its target.
        double firstFree = height + BackLane + Step * backLanes;
        int detours = 0;
        var placed = plans.Select(plan => Connector(Detours.Crosses(plan, byId)
            ? Detours.Detour(plan, byId, firstFree + Step * detours++, geometry)
            : plan, bend)).ToList();
        placed.AddRange(self.Select(edge => Connector(SelfLoop(edge, byId[edge.From].Box, geometry), bend)));

        // Room for the lanes under the drawing, and for a track or a loop past the last column.
        if (back.Count > 0) height += BackLane + Step * (backLanes - 1) + UnderLanes;
        if (detours > 0) height = firstFree + Step * (detours - 1) + UnderLanes;

        // A word reaches along its piece half its length, or laid out turned its lines' height.
        double Reach(PlacedEdge edge) => (edge.Edge.Word, turned) switch
        {
            (null, _) => 0,
            ({ } word, true) => FlowText.WordStep * FlowText.WordLines(word).Count,
            ({ } word, false) => Lying.Width(word) / 2
        };

        double reach = placed.Count == 0 ? 0 : placed.Max(edge => Math.Max(edge.Word.X + Reach(edge), edge.Right));
        return (placed, height, Math.Max(width, reach + PastReach));
    }

    /// <summary>
    /// The forward arrows: each leaves its box's middle, so a box has one line out that branches where it bends, and
    /// arrives at a point of its own on its target's side (Louis: "conformity", "alignment is key"). A word sits on
    /// the piece by its source, or by its target when the source has more than one word out, so two words never
    /// share a line. Between neighbouring columns the first track stands where <see cref="RoomsFor"/> puts it, the
    /// spare room shared either side; an arrow that skips a column bends in the middle.
    /// </summary>
    private static IEnumerable<Plan> Forward(List<GraphEdge> forward,
        IReadOnlyDictionary<string, PlacedNode> byId, bool turned, IGeometry geometry)
    {
        var wordsOut = WordsOut(forward);
        foreach (var edge in forward)
        {
            var from = byId[edge.From];
            var to = byId[edge.To];
            double x1 = from.Box.Right, y1 = from.Box.Middle.Y;
            double x2 = to.Box.Left, y2 = ApartArrows.EntryOf(edge, forward, byId);
            var (track, last) = Track(edge, x1, x2, forward, byId, turned);
            bool bySource = wordsOut.GetValueOrDefault(edge.From) <= 1;
            // An arrow starts and ends on the outline of its boxes' shapes: a decision's end is inside its box
            // everywhere but at its tip.
            double leaves = x1 - NodeShapes.Exit(from, y1, turned), arrives = x2 + NodeShapes.Entry(to, y2, turned);
            // A word stands on its line. Laid out turned it stands beside its line once turned back, and no drop
            // moves it.
            double word = (bySource ? y1 : y2) + (turned ? 0 : ApartArrows.WordDrop);
            // A word by its target stands past the gap's last track, clear of the other sources' lines.
            double along = bySource ? WordAlong(x1, track, turned) : WordAlong(last, x2, turned);
            yield return new Plan(edge,
            [
                geometry.Point(leaves, y1), geometry.Point(track, y1), geometry.Point(track, y2),
                geometry.Point(arrives, y2)
            ], geometry.Point(along, word));
        }
    }

    /// <summary>
    /// Where an arrow bends: between neighbouring columns, after the source's room plus half what the gap has to spare,
    /// then as many steps on as its track; across a column between, in the middle.
    /// </summary>
    private static (double Track, double Last) Track(GraphEdge edge, double x1, double x2, List<GraphEdge> forward,
        IReadOnlyDictionary<string, PlacedNode> byId, bool turned)
    {
        bool Between(PlacedNode node) => node.Box.Left >= x1 && node.Box.Left < x2;
        if (byId.Values.Any(Between)) return ((x1 + x2) / 2, (x1 + x2) / 2);

        // Every arrow from the same column's right edge to the same column's left edge shares this gap.
        var gap = forward.Where(other => byId[other.From].Box.Right == x1 && byId[other.To].Box.Left == x2)
            .ToList();
        var (words, wordsIn) = RoomsFor(gap, forward, turned);
        var (frameOut, frameIn) = FrameRooms(gap, id => byId[id].Node.Group, turned);
        double source = words + frameOut, target = wordsIn + frameIn;
        // How many tracks the gap holds and which one is this arrow's; their room comes off the gap first.
        double tracks = ApartArrows.TracksRoom(gap, forward, byId), room = x2 - x1 - tracks;
        double offset = Step * ApartArrows.TrackOf(edge, gap, forward, byId);
        // A gap narrower than its words' rooms, as a chart that runs down keeps it (CL60.2), bends in its middle; half
        // a pixel's slack, since a gap sized to its rooms exactly can come out a hair short in floating point.
        double first = room + 0.5 < source + target ? x1 + room / 2 : x1 + source + (room - source - target) / 2;
        return (first + offset, first + tracks);
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
        return (leaves ? Bands.Pad + FrameClear : 0,
            enters ? Bands.Pad + FrameClear + (turned ? Bands.LabelRoom : 0) : 0);
    }

    /// <summary>How far outside a group's frame a bend stands.</summary>
    private const double FrameClear = 8;

    /// <summary>
    /// The arrows back to earlier columns: all the arrows into one box share one track past the last column, one lane
    /// under the drawing and one entry in the box's bottom middle. A second target's set is nested outside the first,
    /// so two sets never cross, and the tracks all start past any loop to itself and its word.
    /// </summary>
    private static IEnumerable<Plan> Back(List<GraphEdge> back, IReadOnlyDictionary<string, PlacedNode> byId,
        double height, bool turned, IGeometry geometry)
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
            double outAt = targets.Contains(edge.From) ? Before : Middle;
            double inAt = sources.Contains(edge.To) ? After : Middle;
            double x1 = from.Box.Across(outAt), y1 = from.Box.Bottom, lane = height + BackLane + Step * at;
            double entry = to.Box.Across(inAt), y2 = to.Box.Bottom;
            // It starts and ends on the outline, which under a decision or a pill is higher towards its ends.
            double leaves = y1 - NodeShapes.Under(from, x1, turned), arrives = y2 - NodeShapes.Under(to, entry, turned);
            yield return new Plan(edge,
            [
                geometry.Point(x1, leaves), geometry.Point(x1, lane), geometry.Point(entry, lane),
                geometry.Point(entry, arrives)
            ], geometry.Point((x1 + entry) / 2, lane + WordUnder));
        }
    }

    /// <summary>
    /// An arrow inside a column (a pipeline a turn runs down): a straight drop from the box's bottom middle to the next
    /// box's top middle, its word beside it in the gap. Rising, it climbs the same way, from the top to the bottom.
    /// </summary>
    private static Plan Down(GraphEdge edge, IBox from, IBox to, IGeometry geometry)
    {
        bool falls = to.Top >= from.Top;
        double x = from.Middle.X, y1 = falls ? from.Bottom : from.Top, y2 = falls ? to.Top : to.Bottom;
        return new Plan(edge, [geometry.Point(x, y1), geometry.Point(x, y2)],
            geometry.Point(BesideLine(x, edge.Word), (y1 + y2) / 2 + WordUnder));
    }

    /// <summary>
    /// An arrow from a box to itself: out of its right side near the top, round and back in near the bottom, its word
    /// beside it.
    /// </summary>
    private static Plan SelfLoop(GraphEdge edge, IBox box, IGeometry geometry)
    {
        double x = box.Right, y1 = box.Down(LoopOut), y2 = box.Down(LoopIn), reach = x + SelfReach;
        return new Plan(edge,
        [
            geometry.Point(x, y1), geometry.Point(reach, y1), geometry.Point(reach, y2), geometry.Point(x, y2)
        ], geometry.Point(BesideLine(reach, edge.Word), box.Middle.Y + WordUnder));
    }

    /// <summary>
    /// The way through the points, each bend rounded by a quarter-curve that starts and ends <paramref name="bend"/>
    /// from the corner, or less when a piece is shorter, so a short jog between two wide corners reads as a soft S;
    /// the length is the pieces' sum, close enough for the stroke that draws it.
    /// </summary>
    private static PlacedEdge Connector(Plan plan, double bend)
    {
        var (edge, points, word) = plan;
        var way = ArrowPath.From(points[0]);
        double length = 0;
        for (int at = 1; at < points.Count; at++)
        {
            IPoint from = points[at - 1], to = points[at];
            length += from.StepsTo(to);
            if (at == points.Count - 1)
            {
                way = way.To(to);
                continue;
            }

            // Stop short of the corner, then curve through it towards the next point.
            var next = points[at + 1];
            double round = Math.Min(bend, Math.Min(from.StepsTo(to), to.StepsTo(next)) / 2);
            if (round < 1) continue;
            way = way.To(to.Toward(from, round)).Round(to, to.Toward(next, round));
        }

        return new PlacedEdge(edge, way, length, word, points.Max(point => point.X));
    }
}
