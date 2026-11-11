namespace LouisAgent.Diagrams.Flow.Spec;

/// <summary>
/// One box of a diagram: its id (what an edge names), the words on it, an optional line under them, and the column it
/// stands in, counted from the left; left out, the arrows say it (<see cref="Ranks"/>). It may stand in a group.
/// </summary>
public sealed record GraphNode(
    string Id,
    string Label,
    string? Note = null,
    int? Column = null,
    string? Group = null);
