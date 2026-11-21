namespace LouisAgent.Diagrams.Engine.Cards.Placed;

/// <summary>
/// An overview laid out in the new look (F134.9): its size, its cards and the rails between its rows. It starts at
/// the origin, and the drawing puts its title and its ground round it.
/// </summary>
public sealed record PlacedOverview(
    double Width,
    double Height,
    IReadOnlyList<PlacedCard> Cards,
    IReadOnlyList<PlacedRail> Rails);
