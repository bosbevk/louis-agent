using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Sequence.Spec.Steps;

namespace LouisAgent.Diagrams.Engine.Sequence.Listed.Placed;

/// <summary>
/// One step of a list of steps where it stands: a call, an answer or a note, its two ends, its words broken into
/// lines for its room, the box it takes, and its number when the sequence counts its calls.
/// </summary>
/// <param name="Step">The step from the sequence's data.</param>
/// <param name="Ends">Who sends it and who it goes to, or nothing for a note.</param>
/// <param name="Lines">Its words, broken so no line is wider than its room.</param>
/// <param name="Box">The room it takes on the page: the bar, its ends and its words, or a note's glass.</param>
/// <param name="Number">Its number, when the sequence asks for numbers and the step is not a note.</param>
public sealed record PlacedStep(
    SequenceStep Step,
    PlacedEnds? Ends,
    IReadOnlyList<string> Lines,
    IBox Box,
    int? Number = null);
