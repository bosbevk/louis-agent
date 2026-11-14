using LouisAgent.Diagrams.Sequence.Spec.Steps;

namespace LouisAgent.Diagrams.Engine.Sequence.Placed;

/// <summary>
/// One step's row: its words broken into lines, the lifelines its arrow runs between (the same one for a message to
/// oneself) or its note spans, the band of the drawing it takes, where its arrow runs under its words, and its
/// number in a numbered chart.
/// </summary>
public sealed record PlacedRow(
    SequenceStep Step,
    IReadOnlyList<string> Lines,
    double FromX,
    double ToX,
    double Y,
    double H,
    double ArrowY,
    int? Number = null)
{
    /// <summary>A message to oneself turns back on its own lifeline.</summary>
    public bool Self => Step is MessageStep or ReplyStep && FromX == ToX;
}
