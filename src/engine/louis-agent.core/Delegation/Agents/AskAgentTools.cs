using System.ComponentModel;
using LouisAgent.Composition.Agents;
using LouisAgent.Composition.Messages;
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
/// (<see cref="AgentAddresses"/>). The child opens one level down, so it can't ask further, on the same path as a
/// specialist (<see cref="PeerSession"/>): the key from AGENT_API_KEY, the limit of three a turn, the timeout, the
/// roll-up and the live events.
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
    IUsageSink? usageSink = null)
{
    /// <summary>Asks one turn may make; outside a turn (an MCP call), the most a minute may.</summary>
    public const int MaxPerTurn = 3;

    /// <summary>An asked agent's kind on the registry.</summary>
    public const string Kind = OpenSubSession.AgentKind;

    private readonly TimeSpan _timeout = timeout ?? SpecialistTools.DefaultTimeout;
    private readonly CallSlots _slots = new(MaxPerTurn, SpecialistTools.NoTurnWindow, time ?? TimeProvider.System);

    private readonly PeerSession _session =
        PeerSession.For(http ?? PeerHttp.Shared, apiKey, usageSink, time ?? TimeProvider.System);

    /// <summary>The toolset as the engine builds it, from the instance's peers, key and identities.</summary>
    public AskAgentTools(AgentOptions options, IReadOnlyList<IAgentIdentity> identities, IUsageSink? usageSink = null)
        : this(options.AgentPeers, identities, options.AgentApiKey, usageSink: usageSink)
    {
    }

    /// <summary>Asks the agent at an address and gives back its answer.</summary>
    [Description(
        "Asks another agent by its address and waits for its answer: an agent in its own voice (\"acme\") or one " +
        "of its roles (\"acme.cook\"), only one the agents guide lists. It sees your message and the files you " +
        "name, never this conversation; asked again in this chat, it carries on the conversation you have with it. " +
        "At most 3 asks per turn, 5 minutes each.")]
    public async Task<string> AskAgent(
        [Description("Who answers: the agent's address, as the agents guide lists it.")]
        string address,
        [Description(
            "The question or task, self-contained: the agent sees none of this conversation, only what you asked " +
            "it before.")]
        string message,
        [Description("Names of files the user attached in this conversation that the agent needs.")]
        string[]? attachments = null,
        CancellationToken cancellationToken = default)
    {
        if (Reach(address, out string? refusal) is not { } agent) return refusal!;
        if (ForwardedAttachments.Take(attachments, out var forwarded) is { } unknownAttachment)
            return unknownAttachment;
        if (SpecialistTools.ResolvePeer(peers, null, out Uri peer, out string name) is { } noPeer) return noPeer;

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
            Depth: 1, Parent: parent, Location: UserPlace.Current?.Location, TimeZone: UserPlace.Current?.TimeZone,
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

    /// <summary>The agent an address reaches for this asker, or the reason the model reads instead.</summary>
    /// <remarks>Guard clauses (Fowler), one per check, in the order the model can fix them.</remarks>
    private AddressedAgent? Reach(string address, out string? refusal)
    {
        refusal = null;
        if (!AgentAddress.TryParse(address, out var parsed))
        {
            refusal = MessageText.Format(SubSessionToolMessage.AddressInvalid, new { Address = address });
            return null;
        }

        if (Asker() is not { } asker)
        {
            refusal = MessageText.Format(SubSessionToolMessage.AskerNone);
            return null;
        }

        return AgentCalls.Allows(asker, parsed, out refusal)
            ? AgentAddresses.Resolve(parsed, identities, out refusal)
            : null;
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
