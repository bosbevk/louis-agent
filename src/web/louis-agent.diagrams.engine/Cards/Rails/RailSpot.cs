using LouisAgent.Diagrams.Cards;

namespace LouisAgent.Diagrams.Engine.Cards.Rails;

/// <summary>
/// One end of a rail, across the picture: the card it belongs to, how far from the left it stands, and whether the
/// card is above the rail or below it.
/// </summary>
public sealed record RailSpot(string Card, double X, bool Above);
