using System.ComponentModel;
using LouisAgent.Core.Config;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Sessions;
using LouisAgent.Core.Sessions.Replay;
using LouisAgent.Core.Sessions.SubSessions;
using LouisAgent.Core.Tools.Agents.Messages;
using LouisAgent.Core.Tools.Agents.SubSessions;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Core.Usage.Sinks;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Tools.Agents;

/// <summary>
/// The <c>specialists</c> toolset: hands a task to another louis-agent (a peer in AGENT_PEERS) with only the toolsets
/// the task needs, and gives back its answer. The specialist's tools and context never enter this agent's prompt, and
/// what it used goes to this turn's totals and the ledger, never into the answer.
/// </summary>
/// <remarks>
/// The limits are in code, not in the prompt: at most <see cref="MaxPerTurn"/> sub-sessions per turn
/// (<see cref="CallSlots"/>), each stopped after its timeout, and the child is always deleted unless a chat keeps it.
/// A child is opened with depth 1, which the peer's API refuses the <c>specialists</c> toolset at. The API key comes
/// from AGENT_API_KEY, never from the model's arguments. The child's path (open, ask, keep or delete, roll up) is
/// <see cref="PeerSession"/>, shared with <see cref="MinionTools"/> (F30), which opens a minion at depth 2 the same
/// way; only the body, the limits and the answer's footer differ (<see cref="SubSessionKind"/>).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0031-orchestration-is-an-engine-function.md">ADR 0031: orchestration is an engine function</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0007-measure-every-request.md">ADR 0007: measure every request</seealso>
public sealed class SpecialistTools(
    IReadOnlyDictionary<string, Uri> peers,
    string? apiKey,
    HttpClient? http = null,
    TimeSpan? timeout = null,
    TimeProvider? time = null,
    IUsageSink? usageSink = null)
{
    /// <summary>Sub-sessions one turn may open; outside a turn (an MCP call), the most a minute may.</summary>
    public const int MaxPerTurn = 3;

    /// <summary>The window of the limit outside a turn.</summary>
    public static readonly TimeSpan NoTurnWindow = TimeSpan.FromMinutes(1);

    /// <summary>How long one sub-session may run.</summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(5);

    /// <summary>A sub-session's kind on the registry (F26); a minion's is <see cref="MinionTools.Kind"/>.</summary>
    public const string Kind = OpenSubSession.SpecialistKind;

    private readonly TimeSpan _timeout = timeout ?? DefaultTimeout;
    private readonly CallSlots _slots = new(MaxPerTurn, NoTurnWindow, time ?? TimeProvider.System);

    private readonly PeerSession _session =
        PeerSession.For(http ?? PeerHttp.Shared, apiKey, usageSink, time ?? TimeProvider.System);

    /// <summary>The toolset as the engine builds it, from the instance's peers and key.</summary>
    /// <param name="options">The instance's settings: its peers and its key.</param>
    /// <param name="usageSink">
    /// The engine's ledger, for the roll-up of each sub-session's usage (F5); null writes none.
    /// </param>
    public SpecialistTools(AgentOptions options, IUsageSink? usageSink = null) : this(options.AgentPeers,
        options.AgentApiKey, usageSink: usageSink)
    {
    }

    /// <summary>
    /// Opens a sub-session on a peer, sends it the task and gives back its answer under a footer that names it. In a
    /// chat it stays open for a follow-up; elsewhere it's closed.
    /// </summary>
    [Description(
        "Opens a session on another louis-agent (a peer listed in AGENT_PEERS) with only the toolsets a task needs, " +
        "sends it the task and waits for its final answer. Returns the answer. In a chat the sub-session " +
        "stays open and the answer names its handle, so a follow-up on the same work goes to ContinueSubSession; " +
        "elsewhere it is closed. At most 3 calls per turn (starts and continues), 5 minutes each. See the " +
        "specialists guide for when to delegate.")]
    public async Task<string> StartSubSession(
        [Description("The toolsets the sub-session loads, e.g. [\"files\",\"git\",\"dotnet\"]. Never \"specialists\".")]
        string[] toolsets,
        [Description("The whole task, self-contained: the sub-session sees nothing of this conversation.")]
        string message,
        // Optional, so a call without it is valid: the delegation check saw a router leave it out (the call failed)
        // and guess "default".
        [Description(
            "The peer's name from AGENT_PEERS. Leave it out when this instance has one peer (the usual case); " +
            "it is then used.")]
        string? agent = null,
        [Description(
            "The sub-session's role on the peer: its function (the specialists guide says which goes with which " +
            "toolsets). Empty: the peer's default.")]
        string? role = null,
        [Description(
            "Names of files the user attached in this conversation that the task needs; they are sent with the task " +
            "(the sub-session sees nothing else of the conversation).")]
        string[]? attachments = null,
        [Description(
            "The LLM profile the sub-session runs on, when the specialists guide names one for the task (the peer " +
            "lists them at /v1/llm-profiles). Empty: the peer's own model.")]
        string? llm = null,
        CancellationToken cancellationToken = default)
    {
        if (ForwardAttachments(attachments, out var forwarded) is { } unknownAttachment) return unknownAttachment;
        if (ResolvePeer(peers, agent, out Uri peer, out string name) is { } noPeer) return noPeer;

        UsageScope? scope = UsageScope.Current;
        // The call that opened this sub-session, so a host can show its events under that call.
        string? toolUseId = FunctionInvokingChatClient.CurrentContext?.CallContent.CallId;
        if (_slots.Take(scope, out string? parent) is { } limit) return limit;

        var createBody = new
        {
            toolsets,
            depth = 1,
            task = $"sub-session:{name}",
            run = scope?.Tags.Run,
            service = scope?.Tags.Service,
            parent,
            role = string.IsNullOrWhiteSpace(role) ? null : role.Trim(),
            llm = string.IsNullOrWhiteSpace(llm) ? null : llm.Trim(),
            // The user's place, when they shared it, so the specialist's time and "near me" are theirs (F22).
            location = UserPlace.Current?.Location,
            time_zone = UserPlace.Current?.TimeZone,
            // An eval's web input: the parent's own trial, so the specialist's pages are recorded and replayed with
            // its (F18 step 4a).
            replay = SessionReplay.ForSubSession(),
        };
        // The footer names the specialist, never what it used: those numbers differ on a replay (FX27).
        var kind = new SubSessionKind(Kind, name, $"sub-session:{name}", KeptAnswer, reply =>
            MessageText.Format(SubSessionToolMessage.SubSessionAnswer, new
            {
                Answer = reply.Text,
                Agent = name,
                Toolsets = string.Join(", ", toolsets),
            }));
        return await _session.OpenAsync(peer, name, createBody, new
        {
            message,
            attachments = forwarded
        }, toolsets,
            string.IsNullOrWhiteSpace(role) ? null : role.Trim(), kind, toolUseId, scope, _timeout, cancellationToken);
    }

    /// <summary>
    /// Sends a follow-up to a sub-session this chat kept open, which still has everything it found, and gives back
    /// its answer.
    /// </summary>
    [Description(
        "Sends a follow-up to a sub-session this chat keeps open (the handle from a StartSubSession answer, e.g. " +
        "\"sub-1\") and waits for its answer. It keeps everything it found and read, so use it when the user follows " +
        "up on work that specialist did; start a new sub-session for new work or another role. Counts against the 3 " +
        "calls per turn, 5 minutes.")]
    public async Task<string> ContinueSubSession(
        [Description("The handle from the earlier answer's footer, e.g. \"sub-1\".")]
        string handle,
        [Description("The follow-up, self-contained: what the user now asks, and anything new they said since.")]
        string message,
        [Description("Names of files the user attached in this conversation that the follow-up needs.")]
        string[]? attachments = null,
        CancellationToken cancellationToken = default)
    {
        if (SubSessionRegistry.Current is not { } registry)
            return MessageText.Format(SubSessionToolMessage.SubSessionNoneKept);
        if (registry.Find(handle) is not { } open || open.Kind != Kind)
            return MessageText.Format(SubSessionToolMessage.SubSessionUnknownHandle,
                new
                {
                    Handle = handle.Trim(),
                    Open = OpenList(registry, Kind)
                });
        if (ForwardAttachments(attachments, out var forwarded) is { } unknownAttachment) return unknownAttachment;

        UsageScope? scope = UsageScope.Current;
        string? toolUseId = FunctionInvokingChatClient.CurrentContext?.CallContent.CallId;
        if (_slots.Take(scope, out _) is { } limit) return limit;

        var kind = new SubSessionKind(Kind, open.Agent, $"sub-session:{open.Agent}", KeptAnswer, _ => "");
        return await _session.ContinueAsync(registry, open, new
        {
            message,
            attachments = forwarded
        }, kind, toolUseId,
            scope, _timeout, cancellationToken);
    }

    /// <summary>
    /// Finds the peer the model named. If it named none and there's only one, that one is used, so the model never
    /// has to guess a name; otherwise you get the refusal that lists them.
    /// </summary>
    /// <remarks>Guard clauses (Fowler), one per case, instead of an <c>if</c> inside an <c>if</c>.</remarks>
    internal static string? ResolvePeer(IReadOnlyDictionary<string, Uri> peers, string? agent, out Uri peer,
        out string name)
    {
        peer = null!;
        name = agent?.Trim() ?? "";
        string peerNames = peers.Count == 0 ? "(none)" : string.Join(", ", peers.Keys.Order());
        if (name.Length == 0 && peers.Count != 1)
            return MessageText.Format(SubSessionToolMessage.PeerNameNeeded, new { Peers = peerNames });
        if (name.Length == 0) name = peers.Keys.Single();
        if (!peers.TryGetValue(name, out Uri? found))
            return MessageText.Format(SubSessionToolMessage.UnknownPeer, new
            {
                Agent = name,
                Peers = peerNames
            });

        peer = found;
        return null;
    }

    /// <summary>
    /// The footer under a kept sub-session's answer: its handle, and how to continue it. Never what it used: those
    /// numbers differ on a replay (FX27).
    /// </summary>
    private static string KeptAnswer(OpenSubSession open, SubSessionMessenger.SubSessionReply reply) =>
        MessageText.Format(SubSessionToolMessage.SubSessionAnswerKept, new
        {
            Answer = reply.Text,
            open.Handle,
            open.Agent,
            Toolsets = string.Join(", ", open.Toolsets),
            Role = string.IsNullOrWhiteSpace(open.Role)
                ? MessageText.Format(SubSessionToolMessage.SubSessionDefaultRole)
                : open.Role,
        });

    /// <summary>The kept children of one kind, for a refusal that lists what is open.</summary>
    internal static string OpenList(SubSessionRegistry registry, string kind)
    {
        var open = registry.Open.Where(o => o.Kind == kind).ToList();
        if (open.Count == 0) return MessageText.Format(SubSessionToolMessage.SubSessionNoneOpen);

        string defaultRole = MessageText.Format(SubSessionToolMessage.SubSessionDefaultRole);
        return string.Join(", ",
            open.Select(o => $"{o.Handle} ({o.Agent}: {string.Join(", ", o.Toolsets)}; {o.Role ?? defaultRole})"));
    }

    /// <summary>
    /// The model names the attachments; their contents come from the session, never through the model's output. A
    /// name the session doesn't have gets the refusal that lists what is attached.
    /// </summary>
    private static string? ForwardAttachments(string[]? attachments, out List<object> forwarded)
    {
        var available = SessionAttachments.Current;
        forwarded = [];
        foreach (string name in attachments ?? [])
        {
            if (!available.TryGetValue(name.Trim(), out string? content))
                return MessageText.Format(SubSessionToolMessage.AttachmentUnknown, new
                {
                    Name = name,
                    Attached = available.Count == 0 ? "(none)" : string.Join(", ", available.Keys.Order()),
                });
            forwarded.Add(new
            {
                name = name.Trim(),
                content
            });
        }

        return null;
    }
}
