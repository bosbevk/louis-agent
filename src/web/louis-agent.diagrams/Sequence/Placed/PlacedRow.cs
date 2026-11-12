using LouisAgent.Diagrams.Sequence.Spec.Steps;

namespace LouisAgent.Diagrams.Sequence.Placed;

/// <summary>
/// One step's row: its words broken into lines, the lifelines its arrow runs between (the same one for a message to
/// oneself) or its note spans, the band of the drawing it takes, and its number in a numbered chart.
/// </summary>
public sealed record PlacedRow(
    SequenceStep Step,
    IReadOnlyList<string> Lines,
    double FromX,
    double ToX,
    double Y,
    double H,
    int? Number = null)
{
    /// <summary>Where the arrow runs, under its words.</summary>
    public double ArrowY => Y + SequenceText.TextTop + Lines.Count * SequenceText.LineStep + SequenceText.ArrowGap;

    /// <summary>A message to oneself turns back on its own lifeline.</summary>
    public bool Self => Step is MessageStep or ReplyStep && FromX == ToX;
}
