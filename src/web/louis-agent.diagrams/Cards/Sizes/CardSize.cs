namespace LouisAgent.Diagrams.Cards.Sizes;

/// <summary>
/// How a card of an overview is drawn, as far as its height depends on it: how tall it is, its title's lines and
/// whether they stand under its badge, its text's lines, and whether it is a quiet card.
/// </summary>
public sealed record CardSize(
    double Height,
    IReadOnlyList<string> TitleLines,
    bool TitleBelow,
    IReadOnlyList<string> TextLines,
    bool Quiet);
