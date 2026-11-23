namespace LouisAgent.Core.Delegation.Messages.Holes.Sequences.Answers;

/// <summary>A step left out of a sequence's answer line: where it stood, who it was, and why it failed.</summary>
public sealed record SequenceStepSkippedHoles(int Number, string Address, string Reason);
