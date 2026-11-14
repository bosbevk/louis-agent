namespace LouisAgent.Diagrams.Messages.Holes.Nested;

/// <summary>A neighbour, and the box it stands by that the nested picture does not have.</summary>
public sealed record UnknownNestedBoxHoles(string Neighbour, string To);
