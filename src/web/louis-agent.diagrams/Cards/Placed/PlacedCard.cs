namespace LouisAgent.Diagrams.Cards.Placed;

/// <summary>
/// A card where the layout put it: its box, its words wrapped to fit, and whether its code chip sits under its title
/// because the two don't fit one line. In the new look (F134.9) it also has its title's lines, whether they stand
/// under its badge, and whether it is a quiet card. The old layout sets none of the three.
/// </summary>
public sealed record PlacedCard(
    Card Card,
    double X,
    double Y,
    double W,
    double H,
    IReadOnlyList<string> TextLines,
    bool ChipBelow,
    IReadOnlyList<string>? TitleLines = null,
    bool TitleBelow = false,
    bool Quiet = false);
