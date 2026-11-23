namespace LouisAgent.Core.Delegation.Messages.Holes.Sequences.Rules;

/// <summary>A sequence with too few steps: its name, the fewest it may have, and how many it has.</summary>
public sealed record SequenceTooShortHoles(string Sequence, int Minimum, int Count);
