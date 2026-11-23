namespace LouisAgent.Core.Delegation.Messages.Holes.Sequences.Answers;

/// <summary>
/// A sequence that stopped at a step: its name, the step's place and address, why it failed, and the steps that
/// finished before it.
/// </summary>
public sealed record SequenceStoppedHoles(
    string Sequence,
    int Number,
    int Count,
    string Address,
    string Reason,
    string Finished);
