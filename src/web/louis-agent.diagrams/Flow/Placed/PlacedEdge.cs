using LouisAgent.Diagrams.Flow.Spec;

namespace LouisAgent.Diagrams.Flow.Placed;

/// <summary>
/// An arrow with its path: the SVG path of its connector, straight pieces that bend at right angles, about how long
/// it is (the view needs that to make it draw itself), the point where a word on it sits, on a flat piece clear of
/// the boxes, and how far right it reaches, so the drawing can make room for a loop past the last column.
/// </summary>
public sealed record PlacedEdge(GraphEdge Edge, string Path, double Length, double WordX, double WordY, double Right);
