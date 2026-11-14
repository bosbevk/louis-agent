using LouisAgent.Diagrams.Components.Spec;

namespace LouisAgent.Diagrams.Components.Placed;

/// <summary>
/// A neighbour of the root where the layout put it (F134.7): in a column before or after the root, level with the
/// box it talks to.
/// </summary>
/// <param name="Neighbour">The neighbour as the JSON says it.</param>
/// <param name="X">Its left edge.</param>
/// <param name="Y">Its top edge.</param>
/// <param name="Width">How wide it is.</param>
/// <param name="Height">How tall it is.</param>
/// <param name="NoteLines">Its note, a line at a time.</param>
public sealed record PlacedNeighbour(
    ComponentNeighbour Neighbour,
    double X,
    double Y,
    double Width,
    double Height,
    IReadOnlyList<string> NoteLines)
{
    /// <summary>Its right edge.</summary>
    public double Right => X + Width;

    /// <summary>Its bottom edge.</summary>
    public double Bottom => Y + Height;
}
