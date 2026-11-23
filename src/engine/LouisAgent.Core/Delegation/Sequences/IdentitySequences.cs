using LouisAgent.Composition.Agents;
using LouisAgent.Composition.Messages;
using LouisAgent.Composition.Packs;
using LouisAgent.Composition.Packs.Sequences;
using LouisAgent.Core.Delegation.Addresses;
using LouisAgent.Core.Delegation.Messages;
using LouisAgent.Core.Delegation.Messages.Holes.Sequences.Rules;

namespace LouisAgent.Core.Delegation.Sequences;

/// <summary>
/// An identity's sequences as the engine reads them: whether one keeps the rules a sequence must, and the reason the
/// model reads when it doesn't.
/// </summary>
/// <remarks>
/// Ask the type (ADR 0123): what is known about an identity's sequences lives here, so the runner and the ask tool
/// call one place instead of each working it out. Guard clauses (Fowler), one rule each, in the order a reader
/// fixes them. The rules hold in code, not in a prompt, as the identity's <c>Calls</c> do (ADR 0004).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0117-one-address-for-every-agent-call.md">ADR 0117: one address for every agent call</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0123-ask-the-type.md">ADR 0123: ask the type</seealso>
internal sealed class IdentitySequences(IAgentIdentity identity)
{
    /// <summary>The fewest steps a sequence has: one step is one ask.</summary>
    public const int MinimumSteps = 2;

    /// <summary>The sequences of the identity given.</summary>
    public static IdentitySequences Of(IAgentIdentity identity) => new(identity);

    /// <summary>Why the sequence can't run, or nothing when it keeps every rule.</summary>
    public string? RefusalOf(AgentSequence sequence)
    {
        if (sequence.Steps.Count < MinimumSteps)
        {
            var tooShort = new SequenceTooShortHoles(sequence.Name, MinimumSteps, sequence.Steps.Count);
            return MessageText.Format(SequenceToolMessage.SequenceTooShort, tooShort);
        }

        // Each step in order, with the ones before it, so a step asked twice is named where it comes again.
        var seen = new HashSet<AgentAddress>();
        foreach (string step in sequence.Steps)
            if (StepRefusal(sequence, step, seen) is { } why) return why;
        return null;
    }

    /// <summary>Why one step breaks a rule, or nothing; <paramref name="seen"/> holds the steps before it.</summary>
    /// <remarks>
    /// Guard clauses (Fowler): an address it may call, an agent, not asked before, in no other sequence.
    /// </remarks>
    private string? StepRefusal(AgentSequence sequence, string step, HashSet<AgentAddress> seen)
    {
        var named = new SequenceStepHoles(sequence.Name, step);
        if (!AgentAddress.TryParse(step, out var address) || !AgentCalls.Allows(identity, address, out _))
            return MessageText.Format(SequenceToolMessage.SequenceStepNotCallable, named);
        if (AgentAddresses.IsLookup(address))
            return MessageText.Format(SequenceToolMessage.SequenceStepIsLookup, named);
        if (!seen.Add(address)) return MessageText.Format(SequenceToolMessage.SequenceStepTwice, named);
        if (OtherHolding(sequence, address) is not { } other) return null;

        var shared = new SequenceStepSharedHoles(sequence.Name, step, other.Name);
        return MessageText.Format(SequenceToolMessage.SequenceStepShared, shared);
    }

    /// <summary>Another of the identity's sequences that also asks this address, if one does.</summary>
    private AgentSequence? OtherHolding(AgentSequence sequence, AgentAddress address) =>
        identity.Sequences.FirstOrDefault(other =>
            !string.Equals(other.Name, sequence.Name, StringComparison.Ordinal) && IndexIn(other, address) >= 0);

    /// <summary>Where the address stands in the sequence's steps, from 0, or -1 when it isn't one of them.</summary>
    private static int IndexIn(AgentSequence sequence, AgentAddress address)
    {
        var steps = sequence.Steps.Select(step => AgentAddress.TryParse(step, out var listed) ? listed : null).ToList();
        return steps.IndexOf(address);
    }
}
