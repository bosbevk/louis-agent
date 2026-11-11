using LouisAgent.Diagrams.Flow.Spec;

namespace LouisAgent.Diagrams.Flow.Placed;

/// <summary>A group's frame where the layout drew it: around its boxes, with room for its label on top.</summary>
public sealed record PlacedGroup(GraphGroup Group, double X, double Y, double W, double H);
