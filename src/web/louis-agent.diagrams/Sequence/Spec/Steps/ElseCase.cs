namespace LouisAgent.Diagrams.Sequence.Spec.Steps;

/// <summary>One of an alt's other cases: its label and its steps.</summary>
public sealed record ElseCase(string Label, IReadOnlyList<SequenceStep> Steps);
