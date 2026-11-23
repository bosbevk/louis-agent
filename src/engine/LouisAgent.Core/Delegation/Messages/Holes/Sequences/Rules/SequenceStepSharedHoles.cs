namespace LouisAgent.Core.Delegation.Messages.Holes.Sequences.Rules;

/// <summary>A step two sequences of one identity hold: the sequence, the step, and the other sequence.</summary>
public sealed record SequenceStepSharedHoles(string Sequence, string Step, string Other);
