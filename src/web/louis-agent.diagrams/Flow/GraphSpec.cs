namespace LouisAgent.Diagrams.Flow;

/// <summary>
/// A diagram as data (F111): the boxes and the arrows between them. A page declares one of these and the view draws
/// it, so a diagram is ten lines a test can read instead of a drawing kept by hand.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
public sealed record GraphSpec(IReadOnlyList<GraphNode> Nodes, IReadOnlyList<GraphEdge> Edges);
