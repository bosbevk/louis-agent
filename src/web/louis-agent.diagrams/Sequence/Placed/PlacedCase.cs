namespace LouisAgent.Diagrams.Sequence.Placed;

/// <summary>One of an alt's other cases: the divider's height and the label's lines under it.</summary>
public sealed record PlacedCase(string Label, IReadOnlyList<string> Lines, double Y);
