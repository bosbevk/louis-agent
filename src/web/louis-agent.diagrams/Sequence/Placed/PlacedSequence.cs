namespace LouisAgent.Diagrams.Sequence.Placed;

/// <summary>A sequence laid out: the heads along the top, a row per step under them, and the drawing's size.</summary>
public sealed record PlacedSequence(
    IReadOnlyList<PlacedHead> Heads,
    IReadOnlyList<PlacedRow> Rows,
    double Width,
    double Height);
