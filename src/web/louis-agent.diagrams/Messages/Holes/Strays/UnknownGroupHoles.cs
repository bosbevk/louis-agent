namespace LouisAgent.Diagrams.Messages.Holes.Strays;

/// <summary>A box, and the group it names that the diagram doesn't declare.</summary>
public sealed record UnknownGroupHoles(string Id, string? Group);
