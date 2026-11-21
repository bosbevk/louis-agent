using LouisAgent.Composition.Messages;
using LouisAgent.Core.Delegation.Addresses;
using System.Collections.Concurrent;
using LouisAgent.Core.Delegation.Agents;
using LouisAgent.Core.Sessions;
using LouisAgent.Core.Sessions.Replay;
using LouisAgent.Core.Sessions.SubSessions;
using LouisAgent.Core.Delegation.Messages;
using LouisAgent.Core.Delegation.SubSessions;
using LouisAgent.Core.Usage.Records;
using LouisAgent.SessionStream.Wire.Requests;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Delegation;

/// <summary>
/// What <c>AskAgent</c> does when the address is a minion's (F127.5): it hands the minion one task and one of the
/// asker's own tools on a cheap model, so the page the minion reads never enters the asker's context. Only the
/// minion's short answer comes back; what it used goes to the turn's totals and the ledger.
/// </summary>
/// <remarks>
/// The limits are in code, not in the prompt (Louis's rule: a minion can never do more than the agent that sent it):
/// the tool must be one the asker holds (<see cref="AgentTools"/>) and never one of <see cref="NeverHandedDown"/>; at
/// most <see cref="MaxPerTurn"/> minions across the whole tree of one user turn (one GPU answers one request at a
/// time); each stopped after <see cref="DefaultTimeout"/>, inside the asker's own. The model is configuration: the
/// role's list in <c>MINION_LLMS</c>, cheapest first; <c>stronger</c> asks for the next one (a cascade, FrugalGPT) and
/// can never name a model. It shares <c>AskAgent</c>'s path to the child (<see cref="PeerSession"/>); the address was
/// read and checked against the asker's <c>Calls</c> there.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0117-one-address-for-every-agent-call.md">ADR 0117: one address for every agent call</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0007-measure-every-request.md">ADR 0007: measure every request</seealso>
public sealed class MinionAsk
{
    /// <summary>Minions one user turn may open, counted across every agent of that turn.</summary>
    public const int MaxPerTurn = 16;

    /// <summary>How long one minion may run: inside the asker's five minutes.</summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(2);

    /// <summary>A minion's kind on the registry (F26), beside <see cref="AskAgentTools.Kind"/>.</summary>
    public const string Kind = OpenSubSession.MinionKind;

    /// <summary>
    /// The model name in <c>MINION_LLMS</c> that means this instance's own (Haiku, when a role needs a stronger model).
    /// </summary>
    public const string InstanceLlm = "instance";

    /// <summary>The default role in <c>MINION_LLMS</c>.</summary>
    public const string AnyRole = "*";

    /// <summary>
    /// Tools a minion never gets (F30), whatever its asker holds: delegation (a minion is the end of the tree) and the
    /// engine's self-extension (a minion runs one task with one tool; it doesn't write skills or build tools).
    /// </summary>
    public static readonly IReadOnlySet<string> NeverHandedDown = new HashSet<string>(StringComparer.Ordinal)
    {
        nameof(AskAgentTools.AskAgent),
        nameof(AgentWorkTools.ReadAgentWork),
        "ExecuteSkill",
        "CreateSkillFile",
        "ReloadSkills",
        "CreateTool",
        "ListAgentTools"
    };

    private readonly PeerSession _session;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<string>> _llms;
    private readonly TimeSpan _timeout;
    private readonly ConcurrentDictionary<string, int> _perTree = new(StringComparer.Ordinal);

    /// <summary>The minions <c>AskAgent</c> asks on its own path, with this instance's minion models.</summary>
    internal MinionAsk(PeerSession session, IReadOnlyDictionary<string, IReadOnlyList<string>> llms, TimeSpan? timeout)
    {
        _session = session;
        _llms = llms;
        _timeout = timeout ?? DefaultTimeout;
    }

    /// <summary>
    /// Hands the task and one of the asker's tools to the minion, after the checks in code, and gives back its answer
    /// under a footer that names the minion.
    /// </summary>
    /// <remarks>
    /// Guard clauses (Fowler), one per check, in the order the model can fix them; then the minion opens.
    /// </remarks>
    internal async Task<string> AskAsync(AddressedAgent minion, string? tool, SendMessageRequest task, bool stronger,
        Uri peer, string server, CancellationToken cancellationToken)
    {
        string role = minion.Role!;
        tool = tool?.Trim();
        if (string.IsNullOrEmpty(tool))
            return MessageText.Format(SubSessionToolMessage.MinionNeedsTool,
                new { Address = minion.Address.ToString() });
        if (NeverHandedDown.Contains(tool))
            return MessageText.Format(SubSessionToolMessage.MinionToolNeverHandedDown, new { Tool = tool });
        var mine = AgentTools.Current;
        if (!mine.Contains(tool))
            return MessageText.Format(SubSessionToolMessage.MinionToolNotMine,
                new
                {
                    Tool = tool,
                    Tools = mine.Count == 0 ? "(none)" : string.Join(", ", mine.Order())
                });
        if (ModelFor(role, stronger, out string? llm) is { } noModel) return noModel;

        UsageScope? scope = UsageScope.Current;
        string? toolUseId = FunctionInvokingChatClient.CurrentContext?.CallContent.CallId;
        if (TakeSlot(scope) is { } limit) return limit;

        // One level below the asker; no toolsets: the API loads the one that builds the minion's tool.
        var createBody = new CreateSessionRequest($"minion:{role}", scope?.Tags.Run, scope?.Tags.Service,
            Depth: TurnDepth.Current + 1, Parent: CallSlots.ParentTag(scope), Location: UserPlace.Current?.Location,
            TimeZone: UserPlace.Current?.TimeZone, Llm: llm, Tool: tool, Role: role,
            Replay: SessionReplay.ForSubSession());
        string model = llm ?? InstanceLlm;

        // The footer names the minion, never what it used: those numbers differ on a replay (FX27).
        string Answer(SubSessionMessenger.SubSessionReply reply) =>
            MessageText.Format(SubSessionToolMessage.MinionAnswer, new
            {
                Answer = reply.Text,
                Role = role,
                Tool = tool,
                Llm = model
            });

        var kind = new SubSessionKind(Kind, role, $"minion:{role}", (_, reply) => Answer(reply), Answer, Llm: model);
        // Never kept (F30, the fourth hand check): a chat keeps three children, and kept minions evicted its
        // specialists.
        return await _session.OpenAsync(peer, server, createBody, task, [tool], role, kind, toolUseId, scope,
            _timeout, cancellationToken, keep: false);
    }

    /// <summary>
    /// Picks the minion's model from <c>MINION_LLMS</c>: the role's own list, else the default one, cheapest first, and
    /// the next one when the asker wants a stronger model. No model means the instance's own.
    /// </summary>
    private string? ModelFor(string role, bool stronger, out string? llm)
    {
        llm = null;
        if (!_llms.TryGetValue(role, out var cascade) && !_llms.TryGetValue(AnyRole, out cascade))
            return MessageText.Format(SubSessionToolMessage.MinionNoLlm, new { Role = role });
        int index = stronger ? 1 : 0;
        if (index >= cascade.Count)
            return MessageText.Format(SubSessionToolMessage.MinionNoStronger,
                new
                {
                    Role = role,
                    Models = string.Join(" > ", cascade)
                });
        llm = cascade[index] == InstanceLlm ? null : cascade[index];
        return null;
    }

    /// <summary>
    /// Takes one of the user turn's minion slots. Every agent under that turn counts against the same sixteen, because
    /// each one's turn carries the first turn as its parent.
    /// </summary>
    private string? TakeSlot(UsageScope? scope)
    {
        string tree = scope?.Tags.Parent ?? CallSlots.ParentTag(scope) ?? "(no turn)";
        if (_perTree.Count > 1000) _perTree.Clear();
        return _perTree.AddOrUpdate(tree, 1, (_, n) => n + 1) <= MaxPerTurn
            ? null
            : MessageText.Format(SubSessionToolMessage.MinionLimit, new { Max = MaxPerTurn });
    }
}
