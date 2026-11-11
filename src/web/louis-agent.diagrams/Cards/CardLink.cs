namespace LouisAgent.Diagrams.Cards;

/// <summary>A link between two cards in different rows, by their ids, and which way its arrows point.</summary>
public sealed record CardLink(string From, string To, CardLinkKind Kind = CardLinkKind.Down);
