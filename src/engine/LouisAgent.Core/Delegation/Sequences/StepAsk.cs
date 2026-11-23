using LouisAgent.Composition.Agents;
using LouisAgent.Composition.Messages;
using LouisAgent.Composition.Packs;
using LouisAgent.Core.Delegation.Addresses;
using LouisAgent.Core.Delegation.Addresses.Peers;
using LouisAgent.Core.Delegation.Agents;
using LouisAgent.Core.Delegation.Messages;
using LouisAgent.Core.Delegation.Messages.Holes.Addresses;
using LouisAgent.Core.Delegation.SubSessions;
using LouisAgent.Core.Sessions.SubSessions;
using LouisAgent.Core.Usage.Records;
using LouisAgent.SessionStream.Wire.Requests;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Delegation.Sequences;

/// <summary>
/// Asks one step of a sequence, as one ask of the owning identity: its address must be in that identity's
/// <c>Calls</c>, reach an agent here or on the server it names, and stay within the depth and the chain of asks. The
/// agent opens as a sub-session of the asker's turn, so its usage rolls up and its events stream as any ask's do.
/// </summary>
/// <remarks>
/// Guard clauses (Fowler), one per check, in the order <c>AskAgent</c> makes them, through the same
/// <see cref="AgentCalls"/>, <see cref="AgentAddresses"/> and <see cref="AgentAsks"/>, so a step is held to every
/// rule a single ask is. A step's conversation is not kept for a follow-up: the next run of the sequence starts each step
/// fresh.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0117-one-address-for-every-agent-call.md">ADR 0117: one address for every agent call</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0004-arguments-as-data.md">ADR 0004: arguments as data</seealso>
internal sealed class StepAsk(
    IReadOnlyDictionary<string, Uri> peers,
    IReadOnlyList<IAgentIdentity> identities,
    PeerSession session,
    PeerIdentities servers,
    TimeSpan timeout,
    int maxDepth)
{
    /// <summary>Asks the step at <paramref name="number"/> with the message given, from the chain given.</summary>
    public async Task<StepOutcome> AskAsync(IAgentIdentity owner, int number, string step, string message,
        IReadOnlyList<string> chain, List<AttachmentRequest> attachments, CancellationToken cancellationToken)
    {
        if (Reach(owner, step, out string? refusal) is not { } agent)
            return StepOutcome.Refused(number, step, refusal!);
        if (AgentAsks.OutOfBounds(agent, chain, maxDepth) is { } bound) return StepOutcome.Refused(number, step, bound);
        string? server = AgentAsks.ServerOf(agent.Address, peers);
        if (PeerHttp.ResolvePeer(peers, server, out Uri peer, out string name) is { } noPeer)
            return StepOutcome.Refused(number, step, noPeer);
        if (agent.Address.Peer is not null &&
            await servers.RefusalAsync(name, peer, agent.Address.Identity, cancellationToken) is { } notThere)
            return StepOutcome.Refused(number, step, notThere);

        // The answer is caught where the path writes it, so a failure is told apart from an answer by its absence.
        StepOutcome? answered = null;
        string label = agent.Address.ToString();
        var kind = new SubSessionKind(AskAgentTools.Kind, label, $"agent:{label}",
            (_, reply) => Answered(number, step, reply, out answered),
            reply => Answered(number, step, reply, out answered),
            Address: agent.Address);
        var scope = UsageScope.Current;
        var opening = AgentAsks.Opening(agent, scope, CallSlots.ParentTag(scope));
        string text = await session.OpenAsync(peer, name, opening, AgentAsks.MessageFor(agent, message, attachments),
            [], agent.Role, kind, FunctionInvokingChatClient.CurrentContext?.CallContent.CallId, scope, timeout,
            cancellationToken, keep: false);
        return answered ?? StepOutcome.Refused(number, step, text);
    }

    /// <summary>The agent a step's address reaches for its owner, or the reason it reaches no one.</summary>
    /// <remarks>Guard clauses (Fowler): a readable address, one the owner may call, then who it reaches.</remarks>
    private AddressedAgent? Reach(IAgentIdentity owner, string step, out string? refusal)
    {
        if (!AgentAddress.TryParse(step, out var parsed))
        {
            var invalid = new AddressHoles(step);
            refusal = MessageText.Format(SubSessionToolMessage.AddressInvalid, invalid);
            return null;
        }

        if (!AgentCalls.Allows(owner, parsed, out refusal)) return null;
        if (parsed.Peer is not null) return AgentAddresses.OnAnotherServer(parsed);
        return AgentAddresses.Resolve(parsed, identities, out refusal);
    }

    /// <summary>The step's answer and what it did, kept for the runner; the text is what the path hands back.</summary>
    private static string Answered(int number, string step, SubSessionMessenger.SubSessionReply reply,
        out StepOutcome? answered)
    {
        answered = StepOutcome.Answered(number, step, reply.Text, DidLine.Of(new AgentWork(reply.Events).Calls()));
        return reply.Text;
    }
}
