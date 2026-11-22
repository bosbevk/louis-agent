using LouisAgent.Diagrams.Engine.Sequence.Placed;

namespace LouisAgent.Diagrams.Engine.Sequence.Listed.Placed;

/// <summary>
/// A sequence laid out as steps down a page: each call, answer and note in the order they happen, each block's
/// frame, and the list's size.
/// </summary>
/// <param name="Steps">The calls, answers and notes, top to bottom.</param>
/// <param name="Blocks">The blocks' frames, each after the frames inside it, as the wide layout hands them back.</param>
/// <param name="Width">How wide the list is: the width it was laid out for.</param>
/// <param name="Height">How tall the list is, from its first step's top to its last one's foot.</param>
public sealed record PlacedStepList(
    IReadOnlyList<PlacedStep> Steps,
    IReadOnlyList<PlacedBlock> Blocks,
    double Width,
    double Height);
