using LouisAgent.Diagrams.Sequence.Spec.Steps;

namespace LouisAgent.Diagrams.Sequence.Placed;

/// <summary>
/// A block's frame where it stands: round its rows and the lifelines they touch, its label's lines in the tab at
/// its top, a divider for each other case, and how deep it sits inside other blocks.
/// </summary>
public sealed record PlacedBlock(
    BlockStep Block,
    IReadOnlyList<string> Lines,
    IReadOnlyList<PlacedCase> Cases,
    int Depth,
    double X,
    double Y,
    double W,
    double H);
