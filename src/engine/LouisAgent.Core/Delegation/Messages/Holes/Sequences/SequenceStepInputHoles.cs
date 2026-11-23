namespace LouisAgent.Core.Delegation.Messages.Holes.Sequences;

/// <summary>What a step of a sequence is sent: the first message, and the step before with its answer.</summary>
public sealed record SequenceStepInputHoles(string FirstMessage, string Address, string Answer);
