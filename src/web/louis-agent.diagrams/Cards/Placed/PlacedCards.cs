namespace LouisAgent.Diagrams.Cards.Placed;

/// <summary>
/// An overview laid out: its frame's size (the frame starts at the origin), its cards and links, and where the
/// caption's line sits.
/// </summary>
public sealed record PlacedCards(
    double Width,
    double Height,
    IReadOnlyList<PlacedCard> Cards,
    IReadOnlyList<PlacedLink> Links,
    double CaptionY);
