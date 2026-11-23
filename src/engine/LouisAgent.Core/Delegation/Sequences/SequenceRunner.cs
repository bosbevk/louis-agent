using LouisAgent.Composition.Messages;
using LouisAgent.Composition.Packs;
using LouisAgent.Composition.Packs.Sequences;
using LouisAgent.Core.Config;
using LouisAgent.Core.Delegation.Addresses.Peers;
using LouisAgent.Core.Delegation.Agents;
using LouisAgent.Core.Delegation.Messages;
using LouisAgent.Core.Delegation.Messages.Holes.Asks;
using LouisAgent.Core.Delegation.SubSessions;
using LouisAgent.Core.Sessions;
using LouisAgent.Core.Usage.Sinks;
using LouisAgent.SessionStream.Wire.Requests;

namespace LouisAgent.Core.Delegation.Sequences;

/// <summary>
/// Runs one of the turn's identity's sequences: asks each step in order, hands each the message its
/// <see cref="StepInput"/> says, does what its <see cref="StepFailure"/> says when one fails, and gives back the last
/// answer with a line naming every step.
/// </summary>
/// <remarks>
/// Prompt chaining with a gate between the steps, the "predefined code path" of Anthropic's Building effective agents
/// (2024-12-19): code holds the order, so no model can skip a step or ask them the other way round. Each step goes
/// through <see cref="StepAsk"/>, the same checks one ask keeps. The sequence's own key joins the chain of asks, so no
/// step of it can be the sequence again.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0117-one-address-for-every-agent-call.md">ADR 0117: one address for every agent call</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0031-orchestration-is-an-engine-function.md">ADR 0031: orchestration is an engine function</seealso>
public sealed class SequenceRunner
{
    private readonly IReadOnlyList<IAgentIdentity> _identities;
    private readonly StepAsk _steps;

    /// <summary>
    /// The runner on this instance's peers, their keys and its identities, with the timeout of one ask and the
    /// deepest level an ask may open.
    /// </summary>
    public SequenceRunner(
        IReadOnlyDictionary<string, Uri> peers,
        IReadOnlyList<IAgentIdentity> identities,
        string? apiKey,
        HttpClient? http = null,
        TimeSpan? timeout = null,
        TimeProvider? time = null,
        IUsageSink? usageSink = null,
        IReadOnlyDictionary<string, string>? peerKeys = null,
        int maxDepth = AgentOptions.DefaultAgentMaxDepth)
    {
        _identities = identities;
        var client = http ?? PeerHttp.Shared;
        var clock = time ?? TimeProvider.System;
        var keys = PeerHttp.KeysByPeer(peers, peerKeys ?? new Dictionary<string, string>());
        _steps = new StepAsk(peers, identities, PeerSession.For(client, apiKey, usageSink, clock, keys),
            new PeerIdentities(new PeerHttp(client, apiKey, keys), clock), timeout ?? PeerHttp.DefaultTimeout,
            maxDepth);
    }

    /// <summary>
    /// Runs the sequence for the turn's identity, the first step on <paramref name="message"/>, and gives back the
    /// answer its model reads.
    /// </summary>
    /// <remarks>
    /// Guard clauses (Fowler): an identity to run it for, a chain it isn't in already, then the steps.
    /// </remarks>
    public async Task<string> RunAsync(AgentSequence sequence, string message,
        CancellationToken cancellationToken = default, List<AttachmentRequest>? attachments = null)
    {
        if (AgentAsks.AskerOf(_identities) is not { } owner) return MessageText.Format(SubSessionToolMessage.AskerNone);
        string key = $"{owner.Name}.{sequence.Name}";
        if (TurnChain.Current.Contains(key))
        {
            var inChain = new AskInChainHoles(key, string.Join(" > ", TurnChain.Current));
            return MessageText.Format(SubSessionToolMessage.AskInChain, inChain);
        }

        IReadOnlyList<string> chain = [.. TurnChain.Current, key];
        var done = new List<StepOutcome>();
        for (int index = 0; index < sequence.Steps.Count; index++)
        {
            string input = StepMessage.For(sequence.Input, message, done);
            var outcome = await _steps.AskAsync(owner, index + 1, sequence.Steps[index], input, chain,
                attachments ?? [], cancellationToken);
            if (outcome.Failed) return SequenceAnswer.Stopped(sequence, outcome, done);
            done.Add(outcome);
        }

        return SequenceAnswer.Finished(sequence, done);
    }
}
