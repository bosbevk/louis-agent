using LouisAgent.Diagrams.Flow.Spec;

namespace LouisAgent.Diagrams.Flow.Placed;

/// <summary>A box with its place: its top-left corner and its size, in the SVG's units.</summary>
public sealed record PlacedNode(GraphNode Node, double X, double Y, double W, double H);
