using LouisAgent.Diagrams.Flow.Placed;
using LouisAgent.Diagrams.Flow.Spec;

namespace LouisAgent.Diagrams.Flow.Routing;

/// <summary>
/// Says how a flow's forward arrows run between two columns: how much room their tracks need, which track an arrow
/// bends on, and where on its target's side it arrives.
/// </summary>
/// <remarks>
/// Strategy (Refactoring.Guru), Louis's pick of three (F134.8). The router draws an arrow the same way whichever
/// rule it was handed, so a kind of picture that has not had its turn keeps the joined rule while a flow gets the
/// new one, and the old rule is deleted without touching the router once every kind is replaced. An enum with a
/// branch in the router would have kept both rules inside it for good.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0103-a-diagram-is-data-on-a-row-grid.md">ADR 0103: a diagram is data on a row grid</seealso>
public interface IArrowRule
{
    /// <summary>The room the arrows between two neighbouring columns need for tracks beside the first.</summary>
    double TracksRoom(IReadOnlyList<GraphEdge> between);

    /// <summary>Which of its gap's tracks an arrow bends on, counted from the one nearest the sources.</summary>
    int TrackOf(GraphEdge edge, IReadOnlyList<GraphEdge> gap, IReadOnlyDictionary<string, PlacedNode> byId);

    /// <summary>How far down the drawing an arrow arrives at its target's side.</summary>
    double EntryOf(GraphEdge edge, IReadOnlyList<GraphEdge> forward, IReadOnlyDictionary<string, PlacedNode> byId);

    /// <summary>How long a box's side must be at least for that many arrows to arrive on it.</summary>
    double SideFor(int arrows);
}
