namespace LouisAgent.Diagrams.Engine.Cards.Placed;

/// <summary>
/// An overview laid out in the new look (F134.9): its size, its cards, the rails between its rows and, when it runs
/// down, the trunk its linked cards hang from (F169.5). It starts at the origin, and the drawing puts its title and
/// its ground round it.
/// </summary>
public sealed record PlacedOverview(
    double Width,
    double Height,
    IReadOnlyList<PlacedCard> Cards,
    IReadOnlyList<PlacedRail> Rails,
    IReadOnlyList<PlacedTrunk> Trunks);
