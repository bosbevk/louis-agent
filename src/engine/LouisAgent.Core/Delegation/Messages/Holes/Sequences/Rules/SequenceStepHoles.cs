namespace LouisAgent.Core.Delegation.Messages.Holes.Sequences.Rules;

/// <summary>A sequence that can't run because of a step: its name, and the step as the identity wrote it.</summary>
public sealed record SequenceStepHoles(string Sequence, string Step);
