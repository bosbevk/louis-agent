using LouisAgent.Diagrams.Cards.Links;

namespace LouisAgent.Diagrams.Cards;

/// <summary>
/// An overview picture as data: rows of cards, top to bottom, and the links that join a card to one in another row.
/// </summary>
public sealed record CardsSpec(IReadOnlyList<CardRow> Rows, IReadOnlyList<CardLink> Links);
