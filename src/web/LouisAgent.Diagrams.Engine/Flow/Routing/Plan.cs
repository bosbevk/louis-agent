using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Flow.Spec;

namespace LouisAgent.Diagrams.Engine.Flow.Routing;

/// <summary>An arrow's way before it is drawn: its corners, and the point where its word sits.</summary>
/// <param name="Edge">The arrow as the flow's file has it.</param>
/// <param name="Points">Where it starts, each corner it turns at, and where it ends.</param>
/// <param name="Word">The point its word sits at.</param>
internal sealed record Plan(GraphEdge Edge, IReadOnlyList<IPoint> Points, IPoint Word);
