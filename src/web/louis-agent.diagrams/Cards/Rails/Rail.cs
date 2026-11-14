using LouisAgent.Diagrams.Cards.Links;

namespace LouisAgent.Diagrams.Cards.Rails;

/// <summary>
/// One rail of an overview: the gap it stands in, counted from the top, the cards above it and the cards below it,
/// each in its row's order, and the links it carries, as they were written.
/// </summary>
public sealed record Rail(
    int Gap,
    IReadOnlyList<string> Above,
    IReadOnlyList<string> Below,
    IReadOnlyList<CardLink> Links);
