using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Flow.Spec;

namespace LouisAgent.Diagrams.Engine.Flow.Placed;

/// <summary>A box of a flow with its place: the box the layout gave it, in the SVG's units.</summary>
/// <param name="Node">The box as the flow's file has it.</param>
/// <param name="Box">Where it stands and how big it is.</param>
public sealed record PlacedNode(GraphNode Node, IBox Box);
