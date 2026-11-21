namespace LouisAgent.Diagrams.Messages.Holes.Strays;

/// <summary>A link's two ends, and the one of them no card has.</summary>
public sealed record UnknownCardHoles(string From, string To, string Id);
