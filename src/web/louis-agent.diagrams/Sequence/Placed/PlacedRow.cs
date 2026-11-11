using LouisAgent.Diagrams.Sequence.Spec;

namespace LouisAgent.Diagrams.Sequence.Placed;

/// <summary>
/// One step's row: its words broken into lines, the lifelines its arrow runs between (the same one for a message to
/// oneself), and the band of the drawing it takes.
/// </summary>
public sealed record PlacedRow(
    SequenceStep Step,
    IReadOnlyList<string> Lines,
    double FromX,
    double ToX,
    double Y,
    double H)
{
    /// <summary>Where the arrow runs, under its words.</summary>
    public double ArrowY => Y + SequenceText.TextTop + Lines.Count * SequenceText.LineStep + SequenceText.ArrowGap;

    /// <summary>A message to oneself turns back on its own lifeline.</summary>
    public bool Self => FromX == ToX;
}
