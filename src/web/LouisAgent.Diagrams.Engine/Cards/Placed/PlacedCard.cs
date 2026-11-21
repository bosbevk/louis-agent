using LouisAgent.Diagrams.Cards;
using LouisAgent.Diagrams.Engine.Geometry;

namespace LouisAgent.Diagrams.Engine.Cards.Placed;

/// <summary>
/// A card where the layout put it: its box, its title's lines and whether they stand under its badge, its text's
/// lines, and whether it is a quiet card, one no link touches in a picture that has links.
/// </summary>
public sealed record PlacedCard(
    Card Card,
    IBox Box,
    IReadOnlyList<string> TitleLines,
    bool TitleBelow,
    IReadOnlyList<string> TextLines,
    bool Quiet);
