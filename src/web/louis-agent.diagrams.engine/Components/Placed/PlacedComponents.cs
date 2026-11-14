using LouisAgent.Diagrams.Engine.Components.Placed.Keys;
using LouisAgent.Diagrams.Engine.Components.Placed.Lines;

namespace LouisAgent.Diagrams.Engine.Components.Placed;

/// <summary>
/// A whole component diagram, placed (F134.7): every box, pin, neighbour and line with its place, and how large the
/// drawing is. The renderer draws exactly this and decides no place itself.
/// </summary>
/// <param name="Width">How wide the drawing is.</param>
/// <param name="Height">How tall the drawing is.</param>
/// <param name="Boxes">Every box, each holder before the boxes it holds.</param>
/// <param name="Pins">Every pin, on its box's top edge.</param>
/// <param name="Neighbours">What stands around the root.</param>
/// <param name="Lines">The lines between a neighbour and its box, and from a state to its pins.</param>
/// <param name="Key">How to read the picture: a row for each thing it uses.</param>
public sealed record PlacedComponents(
    double Width,
    double Height,
    IReadOnlyList<PlacedBox> Boxes,
    IReadOnlyList<PlacedPin> Pins,
    IReadOnlyList<PlacedNeighbour> Neighbours,
    IReadOnlyList<PlacedLine> Lines,
    PlacedKey Key);
