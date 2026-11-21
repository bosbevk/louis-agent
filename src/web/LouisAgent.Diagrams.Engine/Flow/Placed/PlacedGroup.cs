using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Flow.Spec;

namespace LouisAgent.Diagrams.Engine.Flow.Placed;

/// <summary>A group's frame where the layout drew it: around its boxes, with room for its label on top.</summary>
/// <param name="Group">The group as the flow's file has it.</param>
/// <param name="Box">The frame.</param>
public sealed record PlacedGroup(GraphGroup Group, IBox Box);
