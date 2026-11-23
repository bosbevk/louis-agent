namespace LouisAgent.Core.Delegation.Messages.Holes.Sequences.Inputs;

/// <summary>One answer of the conversation so far, under the step that gave it.</summary>
public sealed record SequenceStepAnsweredHoles(string Address, string Answer);
