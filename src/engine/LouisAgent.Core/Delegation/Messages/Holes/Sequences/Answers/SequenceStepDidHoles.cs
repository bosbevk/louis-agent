namespace LouisAgent.Core.Delegation.Messages.Holes.Sequences.Answers;

/// <summary>One step of a sequence in its answer's line: where it stood, who it was, and what it did.</summary>
public sealed record SequenceStepDidHoles(int Number, string Address, string Did);
