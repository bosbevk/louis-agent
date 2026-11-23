namespace LouisAgent.Core.Delegation.Messages.Holes.Sequences.Inputs;

/// <summary>What a step given the whole conversation is sent: the first message, and every answer so far.</summary>
public sealed record SequenceStepsSoFarHoles(string FirstMessage, string Answers);
