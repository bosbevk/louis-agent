namespace LouisAgent.Diagrams.Messages.Holes.Types;

/// <summary>A kind of picture, the type it wrote that it can't be, and what it may be.</summary>
public sealed record WrongDiagramTypeHoles(string Kind, string Type, string Types);
