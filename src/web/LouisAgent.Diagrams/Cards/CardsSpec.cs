using LouisAgent.Diagrams.Cards.Links;
using LouisAgent.Diagrams.Flow.Spec;

namespace LouisAgent.Diagrams.Cards;

/// <summary>
/// An overview picture as data: rows of cards, top to bottom, the links that join a card to one in another row, and
/// which way it runs, its rows across unless it says down (F169.5), when each card stands under the one before.
/// </summary>
/// <remarks>
/// The direction is a word in the picture's data, the one a flow and a class chart already say theirs with, read by
/// the same reader into the same enum, so a picture drawn for a narrow screen is the same picture told one more thing.
/// </remarks>
public sealed record CardsSpec(
    IReadOnlyList<CardRow> Rows,
    IReadOnlyList<CardLink> Links,
    FlowDirection Direction = FlowDirection.Right);
