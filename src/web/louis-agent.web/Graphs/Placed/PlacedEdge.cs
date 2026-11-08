namespace LouisAgent.Web.Graphs.Placed;

/// <summary>
/// An arrow with its path: the SVG path from the left box's right edge to the right box's left edge, about how long
/// it is (the view needs that to make it draw itself), and the point near its start where a word on it sits, clear
/// of the boxes it heads for.
/// </summary>
public sealed record PlacedEdge(GraphEdge Edge, string Path, double Length, double WordX, double WordY);
