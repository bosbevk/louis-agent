using LouisAgent.Diagrams.Components.Spec;
using LouisAgent.Diagrams.Engine.Geometry;

namespace LouisAgent.Diagrams.Engine.Components.Placed;

/// <summary>
/// A component's box where the layout put it (F134.7): its place and size, how deep it is nested, its note cut into
/// lines, and the pill of the one thing it holds, when it holds one.
/// </summary>
/// <param name="Component">The box as the JSON says it.</param>
/// <param name="Box">Where it stands and how large it is. Its pins stand on its top edge.</param>
/// <param name="Depth">How many boxes hold it: none for the root.</param>
/// <param name="NoteLines">Its note, a line at a time.</param>
/// <param name="Pill">Where the one thing it holds is drawn. None when it holds none.</param>
public sealed record PlacedBox(
    ComponentBox Component,
    IBox Box,
    int Depth,
    IReadOnlyList<string> NoteLines,
    PlacedPill? Pill);
