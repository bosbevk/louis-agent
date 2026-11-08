namespace LouisAgent.Web.Graphs.Placed;

/// <summary>
/// An arrow with its path: the SVG path from the left box's right edge to the right box's left edge, about how long
/// it is (the view needs that to make it draw itself), and its middle, where a word on it sits.
/// </summary>
public sealed record PlacedEdge(GraphEdge Edge, string Path, double Length, double MidX, double MidY);
