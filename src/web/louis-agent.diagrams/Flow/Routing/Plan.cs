using LouisAgent.Diagrams.Flow.Spec;

namespace LouisAgent.Diagrams.Flow.Routing;

/// <summary>An arrow's way before it is drawn: its corners, and where its word sits.</summary>
internal sealed record Plan(GraphEdge Edge, (double X, double Y)[] Points, double WordX, double WordY);
