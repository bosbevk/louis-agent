using System.ComponentModel;
using LouisAgent.Composition.Agents;
using LouisAgent.Composition.Messages;
using LouisAgent.Core.Delegation.Addresses.Peers;
using LouisAgent.Composition.Packs;
using LouisAgent.Composition.Sessions;
using LouisAgent.Core.Config;
using LouisAgent.Core.Delegation.Addresses;
using LouisAgent.Core.Delegation.Messages;
using LouisAgent.Core.Delegation.SubSessions;
using LouisAgent.Core.Sessions;
using LouisAgent.Core.Sessions.Replay;
using LouisAgent.Core.Sessions.SubSessions;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Core.Usage.Sinks;
using LouisAgent.SessionStream.Wire.Requests;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Delegation.Agents;

/// <summary>
/// The <c>agents</c> toolset (F127.3.1): asks another agent by its address, an identity in its own voice
/// (<c>acme</c>) or one of its roles (<c>acme.cook</c>), and gives back its answer under a line that names it.
/// </summary>
/// <remarks>
/// Every check is in code, before anything is sent: the address is read once (<see cref="AgentAddress"/>), the asker
/// must name it in its <c>Calls</c> (<see cref="AgentCalls"/>), and it must reach an agent here
/// (<see cref="AgentAddresses"/>). The child opens one level down, on the same path as a specialist
/// (<see cref="PeerSession"/>): the key from AGENT_API_KEY, the limit of three a turn, the timeout, the roll-up and the
/// live events. A minion's address takes one of the asker's tools and goes to <see cref="MinionAsk"/>, on its own
/// limits (F127.5: one tool for every call an agent makes).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0117-one-address-for-every-agent-call.md">ADR 0117: one address for every agent call</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0031-orchestration-is-an-engine-function.md">ADR 0031: orchestration is an engine function</seealso>
public sealed class AskAgentTools(
    IReadOnlyDictionary<string, Uri> peers,
    IReadOnlyList<IAgentIdentity> identities,
    string? apiKey,
    HttpClient? http = null,
    TimeSpan? timeout = null,
    TimeProvider? time = null,
    IUsageSink? usageSink = null,
    IReadOnlyDictionary<string, string>? peerKeys = null,
    IReadOnlyDictionary<string, IReadOnlyList<string>>? minionLlms = null,
    TimeSpan? minionTimeout = null)
{
    /// <summary>Asks one turn may make; outside a turn (an MCP call), the most a minute may.</summary>
    public const int MaxPerTurn = 3;

    /// <summary>An asked agent's kind on the registry.</summary>
    public const string Kind = OpenSubSession.AgentKind;

    private readonly TimeSpan _timeout = timeout ?? PeerHttp.DefaultTimeout;
    private readonly CallSlots _slots = new(MaxPerTurn, CallSlots.NoTurnWindow, time ?? TimeProvider.System);

    private readonly PeerSession _session = SessionOf(http, apiKey, usageSink, time, peers, peerKeys);

    /// <summary>A minion's address goes here (F127.5), on a path built the same way, with the minion models.</summary>
    private readonly MinionAsk _minions = new(SessionOf(http, apiKey, usageSink, time, peers, peerKeys),
        minionLlms ?? new Dictionary<string, IReadOnlyList<string>>(), minionTimeout);

    /// <summary>The name this instance goes by among its peers, when the host names more than one (F127.3.1).</summary>
    public const string Self = "self";

    /// <summary>Another server's identities, read with its key before an address on it is asked (F127.3.2).</summary>
    private readonly PeerIdentities _identities = new(
        new PeerHttp(http ?? PeerHttp.Shared, apiKey,
            PeerHttp.KeysByPeer(peers, peerKeys ?? new Dictionary<string, string>())),
        time ?? TimeProvider.System);

    /// <summary>
    /// The toolset as the engine builds it, from the instance's peers, their keys and its identities.
    /// </summary>
    public AskAgentTools(AgentOptions options, IReadOnlyList<IAgentIdentity> identities, IUsageSink? usageSink = null)
        : this(options.AgentPeers, identities, options.AgentApiKey, usageSink: usageSink,
            peerKeys: options.AgentPeerKeys, minionLlms: options.MinionLlms)
    {
    }

    /// <summary>The path to a child: the key for each server, the ledger for the roll-up, the clock.</summary>
    private static PeerSession SessionOf(HttpClient? http, string? apiKey, IUsageSink? usageSink, TimeProvider? time,
        IReadOnlyDictionary<string, Uri> peers, IReadOnlyDictionary<string, string>? peerKeys) =>
        PeerSession.For(http ?? PeerHttp.Shared, apiKey, usageSink, time ?? TimeProvider.System,
            PeerHttp.KeysByPeer(peers, peerKeys ?? new Dictionary<string, string>()));

    /// <summary>Asks the agent at an address and gives back its answer.</summary>
    [Description(
        "Asks another agent by its address and waits for its answer: an agent in its own voice (\"acme\") or one " +
        "of its roles (\"acme.cook\"), on another server after its name (\"partner:acme\"), only one the agents " +
        "guide lists. It sees your message and the files you " +
        "name, never this conversation; asked again in this chat, it carries on the conversation you have with it. " +
        "At most 3 asks per turn, 5 minutes each. A minion's address (\"acme.lookup\") also takes one of your own " +
        "tools, and answers once: at most 16 a user turn, 2 minutes each.")]
    public async Task<string> AskAgent(
        [Description("Who answers: the agent's address, as the agents guide lists it.")]
        string address,
        [Description(
            "The question or task, self-contained: the agent sees none of this conversation, only what you asked " +
            "it before.")]
        string message,
        [Description("Names of files the user attached in this conversation that the agent needs.")]
        string[]? attachments = null,
        [Description(
            "Only for a minion's address: the one tool of yours it uses, e.g. \"FetchUrl\" (never a delegation or " +
            "skills tool), as your role file says.")]
        string? tool = null,
        [Description(
            "Only for a minion's address: true to run the same task on the next, stronger model in its list, when " +
            "the first answer failed your check. Once per task; false the first time.")]
        bool stronger = false,
        CancellationToken cancellationToken = default)
    {
        if (Reach(address, tool, out string? refusal) is not { } agent) return refusal!;
        if (ForwardedAttachments.Take(attachments, out var forwarded) is { } unknownAttachment)
            return unknownAttachment;
        // The server the address names, else this instance: "self" when the host names more than one server.
        string? server = agent.Address.Peer ?? (peers.ContainsKey(Self) ? Self : null);
        if (PeerHttp.ResolvePeer(peers, server, out Uri peer, out string name) is { } noPeer) return noPeer;
        if (agent.Address.Peer is not null &&
            await _identities.RefusalAsync(name, peer, agent.Address.Identity, cancellationToken) is { } notThere)
            return notThere;
        // A minion answers once, on its own limits: it never takes one of the turn's three asks, nor is it kept.
        if (agent.Kind == AddressKind.Minion)
            return await _minions.AskAsync(agent, tool, new SendMessageRequest(message, forwarded), stronger, peer,
                name, cancellationToken);

        var ask = new Ask(agent, new SendMessageRequest(message, forwarded), UsageScope.Current,
            // The call that asked, so a host can show the agent's work under it.
            FunctionInvokingChatClient.CurrentContext?.CallContent.CallId, Asker()?.RelaysAnswersWhole == true);
        if (_slots.Take(ask.Scope, out string? parent) is { } limit) return limit;
        if (SubSessionRegistry.Current is not { } kept || AgentLine.Pick(kept, agent.Address) is not { } open)
            return await StartAsync(ask, peer, name, parent, SubSessionToolMessage.AgentAnswer, cancellationToken);

        // Asked before in this chat: the message goes on that conversation, and one the peer dropped starts again.
        return await _session.ContinueAsync(kept, open, ask.Message, KindOf(ask, SubSessionToolMessage.AgentAnswer),
            ask.ToolUseId, ask.Scope, _timeout, cancellationToken,
            () => StartAsync(ask, peer, name, parent, SubSessionToolMessage.AgentAnswerAnew, cancellationToken));
    }

    /// <summary>
    /// Opens a new conversation with the agent, one level down, as the identity in the role resolved: no toolsets
    /// sent, so a voice loads its own packs.
    /// </summary>
    private Task<string> StartAsync(Ask ask, Uri peer, string name, string? parent, SubSessionToolMessage footer,
        CancellationToken cancellationToken)
    {
        string label = ask.Agent.Address.ToString();
        var opening = new CreateSessionRequest($"agent:{label}", ask.Scope?.Tags.Run, ask.Scope?.Tags.Service,
            Depth: TurnDepth.Current + 1, Parent: parent, Location: UserPlace.Current?.Location,
            TimeZone: UserPlace.Current?.TimeZone,
            Role: ask.Agent.Role, Replay: SessionReplay.ForSubSession(), Identity: ask.Agent.Address.Identity);
        return _session.OpenAsync(peer, name, opening, ask.Message, [], ask.Agent.Role, KindOf(ask, footer),
            ask.ToolUseId, ask.Scope, _timeout, cancellationToken);
    }

    /// <summary>How the path names an asked agent and writes its answer, under the line given.</summary>
    private static SubSessionKind KindOf(Ask ask, SubSessionToolMessage footer)
    {
        string label = ask.Agent.Address.ToString();
        return new SubSessionKind(Kind, label, $"agent:{label}", (_, reply) => Answered(ask, footer, label, reply),
            reply => Answered(ask, footer, label, reply), Address: ask.Agent.Address);
    }

    /// <summary>
    /// The answer, and for an asker that relays answers whole, the end of the turn, so its model isn't asked to write
    /// the answer out again (ADR 0079). An empty answer stays with the model, which says what to do about it.
    /// </summary>
    private static string Answered(Ask ask, SubSessionToolMessage footer, string address,
        SubSessionMessenger.SubSessionReply reply)
    {
        if (ask.Relays && !string.IsNullOrWhiteSpace(reply.Text) &&
            FunctionInvokingChatClient.CurrentContext is { } call)
            call.Terminate = true;
        return Answer(footer, address, reply);
    }

    /// <summary>
    /// The agent an address reaches for this asker, a minion when the address names one here (F127.5), or the reason
    /// the model reads instead.
    /// </summary>
    /// <remarks>
    /// Guard clauses (Fowler), one per check, in the order the model can fix them. A tool says the ask is for a
    /// minion, so a tool with a server is refused before the server is named in <c>Calls</c>: a minion runs here.
    /// </remarks>
    private AddressedAgent? Reach(string address, string? tool, out string? refusal)
    {
        refusal = null;
        bool withTool = !string.IsNullOrWhiteSpace(tool);
        if (!AgentAddress.TryParse(address, out var parsed))
            return Refuse(SubSessionToolMessage.AddressInvalid, address, out refusal);
        if (withTool && parsed.Peer is not null)
            return Refuse(SubSessionToolMessage.MinionOnAnotherServer, address, out refusal);
        if (Asker() is not { } asker)
        {
            refusal = MessageText.Format(SubSessionToolMessage.AskerNone);
            return null;
        }

        if (!AgentCalls.Allows(asker, parsed, out refusal)) return null;
        // An address on another server is that server's to answer for: its identities are read before it is asked.
        if (parsed.Peer is not null) return AgentAddresses.OnAnotherServer(parsed);
        if (AgentAddresses.IsMinion(parsed)) return AgentAddresses.ResolveMinion(parsed, identities, out refusal);
        if (withTool) return Refuse(SubSessionToolMessage.ToolOnlyForMinion, address, out refusal);
        return AgentAddresses.Resolve(parsed, identities, out refusal);
    }

    /// <summary>No agent, with the reason naming the address as the model wrote it.</summary>
    private static AddressedAgent? Refuse(SubSessionToolMessage message, string address, out string? refusal)
    {
        refusal = MessageText.Format(message, new { Address = address });
        return null;
    }

    /// <summary>The identity the running turn speaks for, if this instance runs it.</summary>
    private IAgentIdentity? Asker() =>
        TurnIdentity.Current is { } who
            ? identities.FirstOrDefault(i => string.Equals(i.Name, who.Name, StringComparison.OrdinalIgnoreCase))
            : null;

    /// <summary>
    /// The answer under the line that names who gave it and what this reply did; never what it used (FX27).
    /// </summary>
    private static string Answer(SubSessionToolMessage footer, string address,
        SubSessionMessenger.SubSessionReply reply) =>
        MessageText.Format(footer, new
        {
            Answer = reply.Text,
            Address = address,
            Did = DidLine.Of(new AgentWork(reply.Events).Calls())
        });

    /// <summary>
    /// One ask as it was checked: who answers, what is sent, the turn it belongs to, and whether the asker relays the
    /// answer whole.
    /// </summary>
    private sealed record Ask(
        AddressedAgent Agent,
        SendMessageRequest Message,
        UsageScope? Scope,
        string? ToolUseId,
        bool Relays);
}
