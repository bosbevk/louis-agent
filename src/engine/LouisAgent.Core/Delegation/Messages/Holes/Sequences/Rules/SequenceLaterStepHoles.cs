namespace LouisAgent.Core.Delegation.Messages.Holes.Sequences.Rules;

/// <summary>
/// A later step asked on its own: the address, where it stands, its sequence, and the address that starts it.
/// </summary>
public sealed record SequenceLaterStepHoles(string Address, int Number, string Sequence, string First);
