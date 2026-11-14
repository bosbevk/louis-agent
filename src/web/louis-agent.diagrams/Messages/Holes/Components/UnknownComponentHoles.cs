namespace LouisAgent.Diagrams.Messages.Holes.Components;

/// <summary>A neighbour, and the box it stands by that the component diagram does not have.</summary>
public sealed record UnknownComponentHoles(string Neighbour, string To);
