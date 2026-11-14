namespace LouisAgent.Diagrams.Cards.Placed;

/// <summary>
/// A card where the layout put it: its box, its title's lines and whether they stand under its badge, its text's
/// lines, and whether it is a quiet card, one no link touches in a picture that has links.
/// </summary>
public sealed record PlacedCard(
    Card Card,
    double X,
    double Y,
    double W,
    double H,
    IReadOnlyList<string> TitleLines,
    bool TitleBelow,
    IReadOnlyList<string> TextLines,
    bool Quiet);
