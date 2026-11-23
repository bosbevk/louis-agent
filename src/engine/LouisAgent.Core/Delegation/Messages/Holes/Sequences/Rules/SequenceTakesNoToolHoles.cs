namespace LouisAgent.Core.Delegation.Messages.Holes.Sequences.Rules;

/// <summary>A sequence started with a tool or a stronger model: the address, and the sequence it starts.</summary>
public sealed record SequenceTakesNoToolHoles(string Address, string Sequence);
