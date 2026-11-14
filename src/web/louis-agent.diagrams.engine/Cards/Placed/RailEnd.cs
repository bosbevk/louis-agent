using LouisAgent.Diagrams.Cards;

namespace LouisAgent.Diagrams.Engine.Cards.Placed;

/// <summary>
/// One end of a rail where the layout put it: its card, the point on the card's edge its line runs to, whether the
/// card is above the rail, and whether an arrowhead stands there.
/// </summary>
public sealed record RailEnd(string Card, double X, double Y, bool Above, bool Arrow);
