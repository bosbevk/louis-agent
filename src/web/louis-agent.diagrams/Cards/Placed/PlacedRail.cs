using LouisAgent.Diagrams.Cards.Rails;

namespace LouisAgent.Diagrams.Cards.Placed;

/// <summary>
/// A rail where the layout put it: how far down it stands, from where to where it runs, and its ends on the cards
/// above and below it.
/// </summary>
public sealed record PlacedRail(Rail Rail, double Y, double Left, double Right, IReadOnlyList<RailEnd> Ends);
