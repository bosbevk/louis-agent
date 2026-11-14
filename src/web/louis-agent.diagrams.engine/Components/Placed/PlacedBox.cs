using LouisAgent.Diagrams.Components.Spec;

namespace LouisAgent.Diagrams.Engine.Components.Placed;

/// <summary>
/// A component's box where the layout put it (F134.7): its place and size, how deep it is nested, its note cut into
/// lines, and the pill of the one thing it holds, when it holds one.
/// </summary>
/// <param name="Box">The box as the JSON says it.</param>
/// <param name="X">Its left edge.</param>
/// <param name="Y">Its top edge, which its pins stand on.</param>
/// <param name="Width">How wide it is.</param>
/// <param name="Height">How tall it is.</param>
/// <param name="Depth">How many boxes hold it: none for the root.</param>
/// <param name="NoteLines">Its note, a line at a time.</param>
/// <param name="Pill">Where the one thing it holds is drawn. None when it holds none.</param>
public sealed record PlacedBox(
    ComponentBox Box,
    double X,
    double Y,
    double Width,
    double Height,
    int Depth,
    IReadOnlyList<string> NoteLines,
    PlacedPill? Pill)
{
    /// <summary>Its right edge.</summary>
    public double Right => X + Width;

    /// <summary>Its bottom edge.</summary>
    public double Bottom => Y + Height;
}
