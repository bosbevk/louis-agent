namespace LouisAgent.Diagrams.Flow.Spec;

/// <summary>
/// A diagram as data (F111): the boxes and the arrows between them, and which way it runs, to the right unless it
/// says down (CL60.2). A page declares one of these and the view draws it, so a diagram is ten lines a test can read
/// instead of a drawing kept by hand.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
public sealed record GraphSpec(
    IReadOnlyList<GraphNode> Nodes,
    IReadOnlyList<GraphEdge> Edges,
    FlowDirection Direction = FlowDirection.Right,
    IReadOnlyList<GraphGroup>? Groups = null);
