namespace LouisAgent.Diagrams.Engine.Cards.Rails;

/// <summary>
/// A rail across the picture, before it has a height: its ends, the cards above first, and which height of its
/// gap it stands at, counted from the top.
/// </summary>
public sealed record RailSpan(Rail Rail, int Level, IReadOnlyList<RailSpot> Spots)
{
    /// <summary>Its leftmost end.</summary>
    public double Left => Spots.Min(spot => spot.X);

    /// <summary>Its rightmost end.</summary>
    public double Right => Spots.Max(spot => spot.X);
}
