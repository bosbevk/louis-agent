namespace LouisAgent.Diagrams.Cards.Placed;

/// <summary>
/// A card where the layout put it: its box, its words wrapped to fit, and whether its code chip sits under its title
/// because the two don't fit one line.
/// </summary>
public sealed record PlacedCard(
    Card Card,
    double X,
    double Y,
    double W,
    double H,
    IReadOnlyList<string> TextLines,
    bool ChipBelow);
