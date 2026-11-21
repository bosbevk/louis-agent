using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Engine.Nested.Placed.Lines;

/// <summary>
/// A line in a nested picture, placed (F134.7): the points it goes through, from where it starts to where its
/// arrow ends, each stretch straight across or straight down.
/// </summary>
/// <param name="Kind">What it carries.</param>
/// <param name="Points">Its points, in the direction it flows.</param>
/// <param name="Word">The word written on it. None when it carries none.</param>
/// <param name="Tone">Its colour: that of what it comes from. Plain for a call, an answer and a read.</param>
public sealed record PlacedLine(
    LineKind Kind,
    IReadOnlyList<IPoint> Points,
    string? Word = null,
    DiagramTone Tone = DiagramTone.Plain);
