namespace LouisAgent.Diagrams.Messages.Holes.Strays;

/// <summary>The two cards of a link that skips a row, or stays inside one.</summary>
public sealed record LinkNotBetweenRowsHoles(string From, string To);
