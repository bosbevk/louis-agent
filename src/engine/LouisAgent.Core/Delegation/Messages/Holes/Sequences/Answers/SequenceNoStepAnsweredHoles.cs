namespace LouisAgent.Core.Delegation.Messages.Holes.Sequences.Answers;

/// <summary>A sequence whose every step was left out: its name, and each step with why it failed.</summary>
public sealed record SequenceNoStepAnsweredHoles(string Sequence, string Steps);
