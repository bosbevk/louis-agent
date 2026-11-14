namespace LouisAgent.Diagrams.Messages.Holes.Types;

/// <summary>The kind of picture that must say its type and wrote none, and what it may be.</summary>
public sealed record MissingDiagramTypeHoles(string Kind, string Types);
