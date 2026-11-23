namespace LouisAgent.Core.Delegation.Messages.Holes.Sequences.Answers;

/// <summary>A sequence's answer: the last step's answer, the sequence's name, and each step with what it did.</summary>
public sealed record SequenceAnswerHoles(string Answer, string Sequence, string Steps);
