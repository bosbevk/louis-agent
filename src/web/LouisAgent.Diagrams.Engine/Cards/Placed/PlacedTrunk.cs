using LouisAgent.Diagrams.Engine.Cards.Rails;
using LouisAgent.Diagrams.Engine.Geometry;

namespace LouisAgent.Diagrams.Engine.Cards.Placed;

/// <summary>
/// A picture that runs down hangs its linked cards from a trunk in place of a rail (F169.5): one line down the
/// picture's left from the card above to the last linked card, and a branch from it to each, ending on the card's
/// left edge.
/// </summary>
public sealed record PlacedTrunk(Rail Rail, IPoint Top, IPoint Bottom, IReadOnlyList<RailEnd> Branches);
