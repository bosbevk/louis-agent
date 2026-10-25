namespace LouisAgent.Core.Tools.Agents;

using LouisAgent.Core.Sessions.SubSessions;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Core.Usage.Sinks;
using System.Collections.Concurrent;
using System.ComponentModel;
using LouisAgent.Core.Config;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Sessions;
using LouisAgent.Core.Sessions.Replay;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Roles;
using LouisAgent.Core.Tools.Agents.SubSessions;
using LouisAgent.Core.Tools.Clock;
using Microsoft.Extensions.AI;
using LouisAgent.Core.Tools.Agents.Messages;

/// <summary>
/// The <c>minions</c> toolset (F30): a specialist hands one task and one of its own tools to a minion, a session at
/// depth 2 on this instance (<c>AGENT_PEERS</c> names itself) that runs the minion role on the cheap model
/// <c>MINION_LLMS</c> maps the role to. The page the minion reads never enters the specialist's context: only the
/// minion's short answer does. The child's path (open, ask, keep or delete, roll up) is <see cref="PeerSession"/>, as for the specialists.
/// </summary>
/// <remarks>
/// The limits are in code, not in the prompt (Louis's rule: a minion can never do more than the specialist that sent it):
/// the tool must be one the specialist holds (<see cref="AgentTools"/>) and never one of <see cref="NeverHandedDown"/>;
/// at most <see cref="MaxPerTurn"/> minions across the whole tree of one user turn (one GPU answers one request at a
/// time); each stopped after <see cref="DefaultTimeout"/>, inside the specialist's own. The model is configuration: the
/// role's list in <c>MINION_LLMS</c>, cheapest first; <c>stronger</c> asks for the next one (a cascade, FrugalGPT) and can
/// never name a model.
/// </remarks>
public sealed class MinionTools
{
    /// <summary>Minions one user turn may open, counted across every specialist of that turn.</summary>
    public const int MaxPerTurn = 16;

    /// <summary>How long one minion may run: inside the specialist's five minutes.</summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(2);

    /// <summary>A minion's kind on the registry (F26), beside <see cref="SpecialistTools.Kind"/>.</summary>
    public const string Kind = OpenSubSession.MinionKind;

    /// <summary>The model name in <c>MINION_LLMS</c> that means this instance's own (Haiku, when a role needs a stronger model).</summary>
    public const string InstanceLlm = "instance";

    /// <summary>The default role in <c>MINION_LLMS</c>.</summary>
    public const string AnyRole = "*";

    /// <summary>
    /// Tools a minion never gets (F30), whatever its specialist holds: delegation (a minion is the end of the tree) and
    /// the engine's self-extension (a minion runs one task with one tool; it doesn't write skills or build tools).
    /// </summary>
    public static readonly IReadOnlySet<string> NeverHandedDown = new HashSet<string>(StringComparer.Ordinal)
    {
        nameof(SpecialistTools.StartSubSession), nameof(SpecialistTools.ContinueSubSession), nameof(StartMinion),
        "ExecuteSkill", "CreateSkillFile", "ReloadSkills", "CreateTool", "ListAgentTools",
    };

    private readonly IReadOnlyDictionary<string, Uri> _peers;
    private readonly PeerSession _session;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<string>> _llms;
    private readonly TimeSpan _timeout;
    private readonly ConcurrentDictionary<string, int> _perTree = new(StringComparer.Ordinal);

    /// <param name="usageSink">The engine's ledger, for the roll-up of each minion's usage (F5); null writes none.</param>
    public MinionTools(AgentOptions options, IUsageSink? usageSink = null)
        : this(options.AgentPeers, options.MinionLlms, options.AgentApiKey, usageSink: usageSink)
    {
    }

    public MinionTools(IReadOnlyDictionary<string, Uri> peers, IReadOnlyDictionary<string, IReadOnlyList<string>> llms, string? apiKey,
        HttpClient? http = null, TimeSpan? timeout = null, TimeProvider? time = null, IUsageSink? usageSink = null)
    {
        _llms = llms;
        _timeout = timeout ?? DefaultTimeout;
        _peers = peers;
        _session = PeerSession.For(http ?? PeerHttp.Shared, apiKey, usageSink, time ?? TimeProvider.System);
    }

    [Description("Hands one task and one of your own tools to a minion: a small worker on a cheap model that reads one page or " +
        "does one lookup and answers in its role's fixed shape, so what it reads never enters your context. It answers once " +
        "and closes. At most 16 minions per user turn across all specialists, 2 minutes each. See the minions guide.")]
    public async Task<string> StartMinion(
        [Description("The minion's role, <product>-minion-<function>, as your role file lists them.")] string role,
        [Description("The one tool the minion gets, one of your own, e.g. \"FetchUrl\" (never a delegation or skills tool).")] string tool,
        [Description("The task, self-contained and in English: the URL, the product with its size, the user's town. The minion " +
            "sees nothing else.")] string task,
        [Description("True to run the same task on the next, stronger model in the role's list, when this instance lists one " +
            "(MINION_LLMS) and the first answer failed your check. Once per task; false the first time.")] bool stronger = false,
        CancellationToken cancellationToken = default)
    {
        role = role.Trim();
        tool = tool.Trim();
        if (RoleCatalog.MinionBaseOf(role) is null) return MessageText.Format(SubSessionToolMessage.MinionRoleInvalid, new { Role = role });
        if (NeverHandedDown.Contains(tool)) return MessageText.Format(SubSessionToolMessage.MinionToolNeverHandedDown, new { Tool = tool });
        var mine = AgentTools.Current;
        if (!mine.Contains(tool))
            return MessageText.Format(SubSessionToolMessage.MinionToolNotMine, new { Tool = tool, Tools = mine.Count == 0 ? "(none)" : string.Join(", ", mine.Order()) });
        if (ModelFor(role, stronger, out string? llm) is { } noModel) return noModel;
        if (SpecialistTools.ResolvePeer(_peers, null, out Uri peer, out string agent) is { } noPeer) return noPeer;

        UsageScope? scope = UsageScope.Current;
        string? toolUseId = FunctionInvokingChatClient.CurrentContext?.CallContent.CallId;
        if (TakeSlot(scope) is { } limit) return limit;

        var createBody = new
        {
            depth = 2, role, tool, task = $"minion:{role}", run = scope?.Tags.Run, service = scope?.Tags.Service,
            parent = CallSlots.ParentTag(scope), llm,
            location = UserPlace.Current?.Location, time_zone = UserPlace.Current?.TimeZone,
            replay = SessionReplay.ForSubSession(),
        };
        string model = llm ?? InstanceLlm;
        string Answer(SubSessionMessenger.SubSessionReply reply) =>
            MessageText.Format(SubSessionToolMessage.MinionAnswer, new { Answer = reply.Text, Role = role, Tool = tool, Llm = model, Usage = reply.UsageLine });
        var kind = new SubSessionKind(Kind, role, $"minion:{role}", (_, reply) => Answer(reply), Answer, Llm: model);
        // Never kept (F30, the fourth hand check): a chat keeps three children, and kept minions evicted its specialists.
        return await _session.OpenAsync(peer, agent, createBody, new { message = task }, [tool], role, kind, toolUseId, scope, _timeout,
            cancellationToken, keep: false);
    }

    /// <summary>
    /// The model for <paramref name="role"/> from <c>MINION_LLMS</c>: the role's own list, else <c>*</c>'s, cheapest first;
    /// <paramref name="stronger"/> takes the next one. Null in <paramref name="llm"/> means the instance's own model.
    /// Returns the refusal when the role has no list or no stronger model is left.
    /// </summary>
    private string? ModelFor(string role, bool stronger, out string? llm)
    {
        llm = null;
        if (!_llms.TryGetValue(role, out var cascade) && !_llms.TryGetValue(AnyRole, out cascade))
            return MessageText.Format(SubSessionToolMessage.MinionNoLlm, new { Role = role });
        int index = stronger ? 1 : 0;
        if (index >= cascade.Count)
            return MessageText.Format(SubSessionToolMessage.MinionNoStronger, new { Role = role, Models = string.Join(" > ", cascade) });
        llm = cascade[index] == InstanceLlm ? null : cascade[index];
        return null;
    }

    /// <summary>
    /// Takes one of the user turn's minion slots: the specialist's turn carries the router's turn as its parent, so every
    /// specialist under that turn counts against the same sixteen. Outside a turn: the specialist's own, then none.
    /// </summary>
    private string? TakeSlot(UsageScope? scope)
    {
        string tree = scope?.Tags.Parent ?? CallSlots.ParentTag(scope) ?? "(no turn)";
        if (_perTree.Count > 1000) _perTree.Clear();
        return _perTree.AddOrUpdate(tree, 1, (_, n) => n + 1) <= MaxPerTurn ? null
            : MessageText.Format(SubSessionToolMessage.MinionLimit, new { Max = MaxPerTurn });
    }
}
