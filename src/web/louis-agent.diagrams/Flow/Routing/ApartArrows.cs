using LouisAgent.Diagrams.Flow.Placed;
using LouisAgent.Diagrams.Flow.Spec;

namespace LouisAgent.Diagrams.Flow.Routing;

/// <summary>
/// The rule a flow is drawn by (F134.8): each arrow into a box arrives at a point of its own, a step from the next,
/// so the lines that end at one box run in beside each other and you can follow which box feeds which. The lines
/// of a gap turn on one line, and only one that would lie on another turns a step beside it.
/// </summary>
/// <remarks>
/// One of the two strategies behind <see cref="IArrowRule"/>, the one that replaces <see cref="JoinedArrows"/> kind
/// by kind.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0103-a-diagram-is-data-on-a-row-grid.md">ADR 0103: a diagram is data on a row grid</seealso>
public sealed class ApartArrows : IArrowRule
{
    /// <summary>How far from the end of a side its last point keeps.</summary>
    private const double Margin = 6;

    /// <summary>
    /// How near two lines may come before they count as lying on one another: half a step, so two uprights that end
    /// at neighbouring points of one box, a step apart, are clear.
    /// </summary>
    private const double Near = EdgeRouter.Step / 2;

    /// <summary>
    /// What it costs in the order of the tracks to let two lines lie on one another: more than any crossing.
    /// </summary>
    private const int Lying = 100;

    /// <summary>A step for each track the gap needs beside the first.</summary>
    public double TracksRoom(IReadOnlyList<GraphEdge> gap, IReadOnlyList<GraphEdge> forward,
        IReadOnlyDictionary<string, PlacedNode> byId) =>
        EdgeRouter.Step * Tracks(gap, forward, byId).Values.DefaultIfEmpty(0).Max();

    /// <summary>
    /// An arrow bends on its source's track. A line that runs straight has no bend, and counts as the first.
    /// </summary>
    public int TrackOf(GraphEdge edge, IReadOnlyList<GraphEdge> gap, IReadOnlyList<GraphEdge> forward,
        IReadOnlyDictionary<string, PlacedNode> byId) => Tracks(gap, forward, byId).GetValueOrDefault(edge.From);

    /// <summary>
    /// Each source's track in a gap. The lines of a gap turn on one line (Louis: parallel means straight and level),
    /// and a source gets the track a step beside it only when its upright piece would lie on another's.
    /// </summary>
    /// <remarks>
    /// Channel routing, as a circuit board's tracks are assigned: each line's upright piece is an interval of
    /// heights, intervals that overlap may not share a track, and taking them in order, each on the first track clear
    /// of the ones before it, uses no more tracks than that order needs.
    /// </remarks>
    private Dictionary<string, int> Tracks(IReadOnlyList<GraphEdge> gap, IReadOnlyList<GraphEdge> forward,
        IReadOnlyDictionary<string, PlacedNode> byId)
    {
        // Each source's trunk: where its line leaves, and where its arrows arrive. One with no upright needs no track.
        var trunks = gap.GroupBy(edge => edge.From)
            .Select(arrows => new Trunk(arrows.Key, Middle(byId[arrows.Key]),
                [.. arrows.Select(arrow => EntryOf(arrow, forward, byId))]))
            .Where(trunk => trunk.Bottom - trunk.Top >= 1).ToList();

        var tracks = new Dictionary<string, int>(StringComparer.Ordinal);
        var before = new List<Trunk>();
        foreach (var trunk in Ordered(trunks))
        {
            tracks[trunk.Source] = before.Where(other => !other.ClearOf(trunk))
                .Select(other => tracks[other.Source] + 1).DefaultIfEmpty(0).Max();
            before.Add(trunk);
        }

        return tracks;
    }

    /// <summary>
    /// The trunks from the one that turns first. Each is taken from the lowest up and put where it costs least among
    /// the ones already in, weighed against every one of them. So a fan into a box above it turns top first, each
    /// line inside the next, and one into a box under it turns bottom first.
    /// </summary>
    private static List<Trunk> Ordered(List<Trunk> trunks)
    {
        var order = new List<Trunk>();
        foreach (var trunk in trunks.OrderByDescending(trunk => trunk.Leaves))
        {
            // What a place costs: the ones before it turning before this one, and this one before the ones after.
            int Cost(int at) => order.Take(at).Sum(before => before.CostBefore(trunk))
                                + order.Skip(at).Sum(after => trunk.CostBefore(after));

            // Tried from the end, so where two places cost the same the lower trunk still turns first.
            order.Insert(Enumerable.Range(0, order.Count + 1).Reverse().MinBy(Cost), trunk);
        }

        return order;
    }

    /// <summary>
    /// One source's line across a gap: the height it leaves its box at, and the heights its arrows arrive at. Its
    /// upright piece runs from the highest of them to the lowest.
    /// </summary>
    private sealed record Trunk(string Source, double Leaves, IReadOnlyList<double> Arrives)
    {
        /// <summary>The top of its upright piece.</summary>
        public double Top => Math.Min(Leaves, Arrives.Min());

        /// <summary>The bottom of its upright piece.</summary>
        public double Bottom => Math.Max(Leaves, Arrives.Max());

        /// <summary>
        /// Whether its upright piece and another's could stand on one track without lying on one another.
        /// </summary>
        public bool ClearOf(Trunk other) => Bottom + Near <= other.Top || other.Bottom + Near <= Top;

        /// <summary>
        /// What it costs to turn before another: a crossing where the other's line out passes this upright, one
        /// where an arrow of this one passes the other's upright, and far more where an arrow of this one arrives
        /// level with the other's line out, since those two would lie on one another.
        /// </summary>
        public int CostBefore(Trunk other) =>
            Lying * Arrives.Count(arrives => Math.Abs(arrives - other.Leaves) < Near)
            + (Within(other.Leaves) ? 1 : 0)
            + Arrives.Count(other.Within);

        /// <summary>Whether a height is inside its upright piece, not at an end of it.</summary>
        private bool Within(double height) => height > Top + 1 && height < Bottom - 1;
    }

    /// <summary>
    /// An arrow's own point on its target's side. One arrow alone arrives at the middle. Several are spread round the
    /// middle, a step apart. But an arrow from a box level with its target runs straight in (Louis's pick), and the
    /// others take the points beside it, as long as they then still fit on the side. And no point stands level with
    /// the line out of a box next door that feeds something else: the points then stand half a step aside.
    /// </summary>
    public double EntryOf(GraphEdge edge, IReadOnlyList<GraphEdge> forward,
        IReadOnlyDictionary<string, PlacedNode> byId)
    {
        var target = byId[edge.To];
        double middle = Middle(target);
        var arriving = Arriving(edge.To, forward, byId);
        // An arrow passes straight through a waypoint, so it arrives where it leaves: at the middle.
        if (Ranks.IsWaypoint(edge.To)) return middle;

        // The one that can run straight started at a box at the target's own height, next to it or columns away.
        int level = arriving.FindIndex(arrow => Math.Abs(Middle(byId[Origin(arrow, forward)]) - middle) < 1);
        // It keeps its level only if the points above and below it still stand on the side.
        double reach = target.H / 2 - Margin, step = EdgeRouter.Step;
        bool keeps = level >= 0 && level * step <= reach && (arriving.Count - 1 - level) * step <= reach;
        double first = keeps ? middle - level * step : middle - (arriving.Count - 1) * step / 2;
        return first + Aside(first, arriving, forward, byId) + arriving.IndexOf(edge) * step;
    }

    /// <summary>
    /// How far a box's points step aside: half a step when one of them would stand level with the line out of a box
    /// in the column they come from that feeds something else, since an arrow arriving there and that line out would
    /// lie on one another. They step towards the boxes their arrows come from, and not at all if a point would then
    /// leave the side.
    /// </summary>
    private static double Aside(double first, List<GraphEdge> arriving, IReadOnlyList<GraphEdge> forward,
        IReadOnlyDictionary<string, PlacedNode> byId)
    {
        var target = byId[arriving[0].To];
        var feeders = arriving.Select(arrow => arrow.From).ToHashSet(StringComparer.Ordinal);
        double column = byId[arriving[0].From].X, last = first + (arriving.Count - 1) * EdgeRouter.Step;

        // The lines out next door that are not this box's own arrows.
        var others = forward.Where(arrow => !feeders.Contains(arrow.From) && byId[arrow.From].X == column)
            .Select(arrow => Middle(byId[arrow.From])).Distinct().ToList();
        bool Level(double shift) => Enumerable.Range(0, arriving.Count).Any(at =>
            others.Any(other => Math.Abs(first + shift + at * EdgeRouter.Step - other) < 1));
        bool Fits(double shift) => first + shift >= target.Y + Margin && last + shift <= target.Y + target.H - Margin;
        if (!Level(0)) return 0;

        double towards = feeders.Average(feeder => Middle(byId[feeder])) < Middle(target) ? -Near : Near;
        return new[] { towards, -towards }.Where(shift => Fits(shift) && !Level(shift)).FirstOrDefault(0);
    }

    /// <summary>
    /// A side holds its points with a margin at each end, and as many below the middle as above it, so the points
    /// fit whether they are spread round the middle or stand beside a straight arrow at it.
    /// </summary>
    public double SideFor(int arrows) => arrows < 2 ? 0 : 2 * (arrows / 2) * EdgeRouter.Step + 2 * Margin;

    /// <summary>Two lines of different boxes keep half a step apart, so neither is taken for the other.</summary>
    public double LineGap => Near;

    /// <summary>
    /// A word stands on its own line, the line through the middle of its one-line pill. Above the line, the word of
    /// the lower of two arrows into one box would sit on the upper arrow, a step away.
    /// </summary>
    public double WordDrop => FlowText.ChipRise - (FlowText.WordStep + FlowText.ChipExtra) / 2;

    /// <summary>
    /// Boxes that stand level with what feeds them often stand in a long arrow's way at both its heights, and round
    /// the outside of the column is the long way, so it passes between two of them.
    /// </summary>
    public bool PassesBetween => true;

    /// <summary>
    /// The arrows into a box from the top point down: by the height of the box each comes from, the upper source
    /// first, so two lines into one box don't cross on the way in. An arrow past a column comes in level with its
    /// target, so it counts as level among the boxes next door, and among the level ones each stands on the side it
    /// started from: one from higher up above a straight arrow, one from lower down under it.
    /// </summary>
    private static List<GraphEdge> Arriving(string to, IReadOnlyList<GraphEdge> forward,
        IReadOnlyDictionary<string, PlacedNode> byId)
    {
        double middle = Middle(byId[to]);
        return
        [
            .. forward.Where(arrow => arrow.To == to)
                .OrderBy(arrow => Ranks.IsWaypoint(arrow.From) ? middle : Middle(byId[arrow.From]))
                .ThenBy(arrow => Middle(byId[Origin(arrow, forward)]))
                .ThenBy(arrow => Ranks.IsWaypoint(arrow.From))
        ];
    }

    /// <summary>The real box an arrow started at, back through any waypoints it passed.</summary>
    private static string Origin(GraphEdge arrow, IReadOnlyList<GraphEdge> forward)
    {
        string from = arrow.From;
        while (Ranks.IsWaypoint(from)) from = forward.First(link => link.To == from).From;
        return from;
    }

    /// <summary>The height of a box's middle.</summary>
    private static double Middle(PlacedNode node) => node.Y + node.H / 2;
}
