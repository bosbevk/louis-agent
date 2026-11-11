namespace LouisAgent.Diagrams.Cards;

/// <summary>One row of an overview: its cards, left to right, which share the row's width.</summary>
public sealed record CardRow(IReadOnlyList<Card> Cards);
