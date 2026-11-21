namespace LouisAgent.Diagrams.Messages.Holes.Strays;

/// <summary>A relation's two ends, and the one of them no type has.</summary>
public sealed record UnknownTypeHoles(string From, string To, string Id);
