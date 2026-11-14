using LouisAgent.Diagrams.Flow.Placed;
using LouisAgent.Diagrams.Flow.Spec;

namespace LouisAgent.Diagrams.Flow.Routing;

/// <summary>
/// The rule every picture was drawn by before a flow got one of its own: the arrows into a box join at the middle
/// of its side, and a gap has a track for each source only when several boxes feed several.
/// </summary>
/// <remarks>
/// One of the two strategies behind <see cref="IArrowRule"/>. It stays only while a kind of picture still draws
/// with it, and goes when the last one is replaced.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0103-a-diagram-is-data-on-a-row-grid.md">ADR 0103: a diagram is data on a row grid</seealso>
public sealed class JoinedArrows : IArrowRule
{
    /// <summary>
    /// The room a gap's arrows need for tracks of their own: none when one box feeds the rest or all feed one box,
    /// where they join on one track (Louis: "conformity"); otherwise a step for each source after the first. How
    /// high the boxes stand makes no difference to it.
    /// </summary>
    public double TracksRoom(IReadOnlyList<GraphEdge> gap, IReadOnlyList<GraphEdge> forward,
        IReadOnlyDictionary<string, PlacedNode> byId)
    {
        int sources = gap.Select(edge => edge.From).Distinct().Count();
        bool joined = sources <= 1 || gap.Select(edge => edge.To).Distinct().Count() == 1;
        return joined ? 0 : EdgeRouter.Step * (sources - 1);
    }

    /// <summary>
    /// An arrow bends on its source's track: several sources feeding several boxes each have one, top to bottom
    /// (CL60.2), so a reader can follow who feeds whom. A gap that needs no second track has them all on the first.
    /// </summary>
    public int TrackOf(GraphEdge edge, IReadOnlyList<GraphEdge> gap, IReadOnlyList<GraphEdge> forward,
        IReadOnlyDictionary<string, PlacedNode> byId) =>
        TracksRoom(gap, forward, byId) == 0 ? 0 : TrackOrder(gap, byId).IndexOf(edge.From);

    /// <summary>Every arrow arrives at the middle of its target's side, so the arrows into a box join there.</summary>
    public double EntryOf(GraphEdge edge, IReadOnlyList<GraphEdge> forward,
        IReadOnlyDictionary<string, PlacedNode> byId) => byId[edge.To].Y + byId[edge.To].H / 2;

    /// <summary>They all arrive at one point, so a side is long enough whatever arrives.</summary>
    public double SideFor(int arrows) => 0;

    /// <summary>Arrows that join may lie on one another, so no line is ever too near another.</summary>
    public double LineGap => 0;

    /// <summary>A word stands above its line, where it always stood.</summary>
    public double WordDrop => 0;

    /// <summary>A long arrow stays on the row it was given, where it always ran.</summary>
    public bool PassesBetween => false;

    /// <summary>
    /// The order of a gap's tracks, nearest the sources first: the bottom source first, and a source level with
    /// another's target always before that other, so its line out ends before the other's line in starts and the two
    /// never lie on one another (Louis, of the evals chart).
    /// </summary>
    private static List<string> TrackOrder(IReadOnlyList<GraphEdge> gap, IReadOnlyDictionary<string, PlacedNode> byId)
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
}
