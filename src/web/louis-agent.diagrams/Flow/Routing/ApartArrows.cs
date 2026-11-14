using LouisAgent.Diagrams.Flow.Placed;
using LouisAgent.Diagrams.Flow.Spec;

namespace LouisAgent.Diagrams.Flow.Routing;

/// <summary>
/// The rule a flow is drawn by (F134.8): each arrow into a box arrives at a point of its own, a step from the next,
/// so the lines that end at one box run in beside each other and you can follow which box feeds which.
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

    /// <summary>The joined rule, whose tracks this one shares.</summary>
    private static readonly JoinedArrows Joined = new();

    /// <summary>A gap's tracks need the room the joined rule gives them.</summary>
    public double TracksRoom(IReadOnlyList<GraphEdge> between) => Joined.TracksRoom(between);

    /// <summary>An arrow bends on the track the joined rule gives its source.</summary>
    public int TrackOf(GraphEdge edge, IReadOnlyList<GraphEdge> gap, IReadOnlyDictionary<string, PlacedNode> byId) =>
        Joined.TrackOf(edge, gap, byId);

    /// <summary>
    /// An arrow's own point on its target's side. One arrow alone arrives at the middle. Several are spread round the
    /// middle, a step apart. But an arrow from a box level with its target runs straight in (Louis's pick), and the
    /// others take the points beside it, as long as they then still fit on the side.
    /// </summary>
    public double EntryOf(GraphEdge edge, IReadOnlyList<GraphEdge> forward,
        IReadOnlyDictionary<string, PlacedNode> byId)
    {
        var target = byId[edge.To];
        double middle = Middle(target);
        var arriving = Arriving(edge.To, forward, byId);
        if (arriving.Count < 2) return middle;

        // The one that can run straight started at a box at the target's own height, next to it or columns away.
        int level = arriving.FindIndex(arrow => Math.Abs(Middle(byId[Origin(arrow, forward)]) - middle) < 1);
        // It keeps its level only if the points above and below it still stand on the side.
        double reach = target.H / 2 - Margin, step = EdgeRouter.Step;
        bool keeps = level >= 0 && level * step <= reach && (arriving.Count - 1 - level) * step <= reach;
        double first = keeps ? middle - level * step : middle - (arriving.Count - 1) * step / 2;
        return first + arriving.IndexOf(edge) * step;
    }

    /// <summary>
    /// A side holds its points with a margin at each end, and as many below the middle as above it, so the points
    /// fit whether they are spread round the middle or stand beside a straight arrow at it.
    /// </summary>
    public double SideFor(int arrows) => arrows < 2 ? 0 : 2 * (arrows / 2) * EdgeRouter.Step + 2 * Margin;

    /// <summary>
    /// The arrows into a box from the top point down: by the height of the box each comes from, the upper source
    /// first, so two lines into one box don't cross on the way in. An arrow past a column comes in level with its
    /// target, so it counts as level, after a real box that is, and two such by where they started.
    /// </summary>
    private static List<GraphEdge> Arriving(string to, IReadOnlyList<GraphEdge> forward,
        IReadOnlyDictionary<string, PlacedNode> byId)
    {
        double middle = Middle(byId[to]);
        return
        [
            .. forward.Where(arrow => arrow.To == to)
                .OrderBy(arrow => Ranks.IsWaypoint(arrow.From) ? middle : Middle(byId[arrow.From]))
                .ThenBy(arrow => Ranks.IsWaypoint(arrow.From))
                .ThenBy(arrow => Middle(byId[Origin(arrow, forward)]))
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
