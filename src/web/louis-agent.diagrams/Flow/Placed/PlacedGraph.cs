namespace LouisAgent.Diagrams.Flow.Placed;

/// <summary>
/// A diagram with every box, arrow and group given its place, ready to draw: the SVG's size and its parts.
/// </summary>
public sealed record PlacedGraph(
    int Width,
    int Height,
    IReadOnlyList<PlacedNode> Nodes,
    IReadOnlyList<PlacedEdge> Edges,
    IReadOnlyList<PlacedGroup> Groups);
