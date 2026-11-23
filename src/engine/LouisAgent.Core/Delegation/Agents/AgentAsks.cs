using LouisAgent.Composition.Agents;
using LouisAgent.Composition.Messages;
using LouisAgent.Composition.Packs;
using LouisAgent.Composition.Sessions;
using LouisAgent.Core.Delegation.Addresses;
using LouisAgent.Core.Delegation.Messages;
using LouisAgent.Core.Delegation.Messages.Holes.Asks;
using LouisAgent.Core.Sessions;
using LouisAgent.Core.Sessions.Replay;
using LouisAgent.Core.Usage.Records;
using LouisAgent.SessionStream.Wire.Requests;

namespace LouisAgent.Core.Delegation.Agents;

/// <summary>
/// What every ask of an agent shares, whether the model asked it or a sequence did: who is asking, which server
/// answers, whether the ask goes too deep or round in a loop, and what opens the child and what it is sent.
/// </summary>
/// <remarks>
/// Extract Class (Fowler): the checks lived in <see cref="AskAgentTools"/>, and a sequence's steps need the very same
/// ones. One home means a step can never be let through a rule a single ask keeps.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0117-one-address-for-every-agent-call.md">ADR 0117: one address for every agent call</seealso>
internal static class AgentAsks
{
    /// <summary>The identity the running turn speaks for, if this instance runs it.</summary>
    public static IAgentIdentity? AskerOf(IReadOnlyList<IAgentIdentity> identities) =>
        TurnIdentity.Current is { } who
            ? identities.FirstOrDefault(i => string.Equals(i.Name, who.Name, StringComparison.OrdinalIgnoreCase))
            : null;

    /// <summary>The server the address names, else this instance: "self" when the host names more than one.</summary>
    public static string? ServerOf(AgentAddress address, IReadOnlyDictionary<string, Uri> peers) =>
        address.Peer ?? (peers.ContainsKey(AskAgentTools.Self) ? AskAgentTools.Self : null);

    /// <summary>
    /// Why an ask would go too far: past the deepest level this instance allows, or to an agent already in the chain
    /// above, which would go round in a loop. Another server keeps its own chain.
    /// </summary>
    /// <remarks>Guard clauses (Fowler), the depth first: it holds for every ask.</remarks>
    public static string? OutOfBounds(AddressedAgent agent, IReadOnlyList<string> chain, int maxDepth)
    {
        string address = agent.Address.ToString();
        if (TurnDepth.Current + 1 > maxDepth)
        {
            var tooDeep = new AskTooDeepHoles(address, maxDepth);
            return MessageText.Format(SubSessionToolMessage.AskTooDeep, tooDeep);
        }

        if (agent.Address.Peer is not null || !chain.Contains(AgentAddresses.ChainKeyOf(agent))) return null;
        var inChain = new AskInChainHoles(address, string.Join(" > ", chain));
        return MessageText.Format(SubSessionToolMessage.AskInChain, inChain);
    }

    /// <summary>
    /// What opens a new conversation with the agent, one level down, as the identity in the role resolved: no
    /// toolsets sent, so a voice loads its own packs.
    /// </summary>
    public static CreateSessionRequest Opening(AddressedAgent agent, UsageScope? scope, string? parent) =>
        new($"agent:{agent.Address}", scope?.Tags.Run, scope?.Tags.Service,
            Depth: TurnDepth.Current + 1, Parent: parent, Location: UserPlace.Current?.Location,
            TimeZone: UserPlace.Current?.TimeZone, Role: agent.Role, Replay: SessionReplay.ForSubSession(),
            Identity: agent.Address.Identity);

    /// <summary>
    /// The message with the files named; a voice whose window is on the visitor's screen also gets the page it sits
    /// in, as its window would send.
    /// </summary>
    public static SendMessageRequest MessageFor(AddressedAgent agent, string message,
        List<AttachmentRequest> attachments)
    {
        var page = agent.Kind == AddressKind.Identity ? WindowPages.For(agent.Address.Identity) : null;
        return new SendMessageRequest(message, attachments, page);
    }
}
