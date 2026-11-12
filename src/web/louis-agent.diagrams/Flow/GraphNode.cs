namespace LouisAgent.Diagrams.Flow;

/// <summary>
/// One box of a diagram: its id (what an edge names), the words on it, an optional line under them, and the column it
/// stands in, counted from the left.
/// </summary>
public sealed record GraphNode(string Id, string Label, string? Note, int Column);
