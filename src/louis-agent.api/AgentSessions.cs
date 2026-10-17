using System.Collections.Concurrent;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Sessions;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Roles;
using LouisAgent.Core.Tools.Agents;
using LouisAgent.Core.Tools.Clock;
using LouisAgent.Core.Usage;
using Microsoft.Extensions.AI;

namespace LouisAgent.Api;

/// <summary>
/// One conversation: its engine (the toolsets it chose, fixed for its lifetime, F5), its history and the turn in
/// progress, if any.
/// </summary>
internal sealed class AgentSession(string id, AgentEngine engine, IReadOnlyList<string> toolsets, int depth, UsageTags tags,
    string? personality = null, UserPlace? place = null, string? llm = null, string? tool = null)
{
    private readonly SemaphoreSlim _turnLock = new(1, 1);
    private CancellationTokenSource? _activeTurn;

    public string Id { get; } = id;

    /// <summary>The engine of this session's toolset profile, shared with every session that chose the same toolsets.</summary>
    public AgentEngine Engine { get; } = engine;

    /// <summary>The toolsets the session loaded, normalised (sorted, lowercase).</summary>
    public IReadOnlyList<string> Toolsets { get; } = toolsets;

    /// <summary>0 for a session a client opened; 1 for a sub-session another agent opened (F5-S5).</summary>
    public int Depth { get; } = depth;

    /// <summary>The personality the session chose (F21), or null for the default voice (CL9).</summary>
    public string? Personality { get; } = personality;

    /// <summary>Where the user is, when they shared it (F22): set for every turn; the first message says it.</summary>
    public UserPlace? Place { get; } = place;

    /// <summary>The LLM profile the session runs on (F30), or null for the instance's own model.</summary>
    public string? Llm { get; } = llm;

    /// <summary>A minion's one tool (F30, depth 2); null for every other session, which has toolsets.</summary>
    public string? Tool { get; } = tool;

    /// <summary>The longest attachment kept, as in the prompt: one file can't fill a specialist's context.</summary>
    public const int MaxAttachmentChars = 100_000;

    private readonly ConcurrentDictionary<string, string> _attachments = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The files attached in this conversation, by name, for a specialist (F22, <see cref="Core.Tools.Agents.SessionAttachments"/>).</summary>
    public IReadOnlyDictionary<string, string> Attachments => _attachments;

    /// <summary>
    /// The specialists this chat keeps open for a follow-up (F26): current for each turn, so <c>StartSubSession</c> keeps
    /// them and <c>ContinueSubSession</c> finds them; closed on the peer when the chat is removed or dropped as idle.
    /// </summary>
    public SubSessionRegistry SubSessions { get; } = new();

    /// <summary>Keeps an attachment for later turns; the same name again replaces it.</summary>
    public void Remember(string name, string? content)
    {
        string text = content ?? string.Empty;
        _attachments[name] = text.Length <= MaxAttachmentChars ? text : text[..MaxAttachmentChars];
    }

    public List<ChatMessage> History { get; } = engine.NewHistory();

    /// <summary>What the caller said the session is for (e.g. the orchestrator's fix and run); on every usage record.</summary>
    public UsageTags Tags { get; } = tags;
    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastActivity { get; private set; } = DateTimeOffset.UtcNow;
    public bool IsBusy => _turnLock.CurrentCount == 0;

    /// <summary>Messages started in this session; the current one's number is the usage ledger's turn.</summary>
    public int Turns { get; private set; }

    /// <summary>The usage of the session's finished turns, for /usage and GET /sessions/{id} (F3), in the display currency (F14).</summary>
    public UsageSession Usage { get; } = new(engine.Display);

    /// <summary>Starts a turn unless one is already running; the turn stops if the client disconnects.</summary>
    public bool TryStartTurn(CancellationToken requestAborted, out CancellationTokenSource turn)
    {
        turn = null!;
        if (!_turnLock.Wait(0)) return false;

        turn = CancellationTokenSource.CreateLinkedTokenSource(requestAborted);
        _activeTurn = turn;
        Turns++;
        LastActivity = DateTimeOffset.UtcNow;
        return true;
    }

    public void EndTurn(CancellationTokenSource turn)
    {
        Interlocked.CompareExchange(ref _activeTurn, null, turn);
        turn.Dispose();
        LastActivity = DateTimeOffset.UtcNow;
        _turnLock.Release();
    }

    /// <summary>Stops the turn in progress; returns false when there is none.</summary>
    public bool Cancel()
    {
        if (Volatile.Read(ref _activeTurn) is not { } turn) return false;
        try { turn.Cancel(); }
        catch (ObjectDisposedException) { return false; }
        return true;
    }
}

/// <summary>
/// In-memory sessions (like the ACP server); idle ones are dropped so memory doesn't grow forever. Each session gets the
/// engine of the toolsets it asks for (F5), from <see cref="AgentEngineFactory"/>.
/// </summary>
internal sealed class AgentSessions(AgentEngineFactory engines)
{
    /// <summary>What a session created without <c>toolsets</c> loads (F5 decision: self-extension only).</summary>
    public static readonly IReadOnlyList<string> DefaultToolsets = ["skills"];

    /// <summary>The deepest session: a specialist (1) a level-0 session delegates to, a minion (2) a specialist hands one task to (F30).</summary>
    public const int MaxDepth = 2;

    /// <summary>A minion's depth (F30): one role on the minion base, one tool, never delegates.</summary>
    public const int MinionDepth = 2;

    /// <summary>The toolset a specialist hands minions with (F30): a specialist's only, never the chat's nor a minion's.</summary>
    public const string MinionsToolset = "minions";

    /// <summary>The toolset that opens sub-sessions (F5; named specialists in CL9); refused at <see cref="MaxDepth"/>.</summary>
    public const string SpecialistsToolset = "specialists";

    /// <summary>A session nobody has used for this long is dropped at the next create, with the sub-sessions it kept.</summary>
    public static readonly TimeSpan IdleTimeout = TimeSpan.FromHours(4);

    private readonly ConcurrentDictionary<string, AgentSession> _sessions = new();

    /// <summary>
    /// Opens a session with <paramref name="toolsets"/> (null: <see cref="DefaultToolsets"/>; empty: none) and
    /// <paramref name="personality"/> (null: the default voice). The session's level is its depth (CL8): its personality comes
    /// from that level's folder, and a sub-session gets no router (default.md): its toolsets were chosen for it and it
    /// can't delegate (F21). Throws <see cref="ToolsetException"/> for an unknown or unavailable
    /// toolset, a depth outside 0..<see cref="MaxDepth"/>, or a sub-session asking for <see cref="SpecialistsToolset"/>, and
    /// <see cref="Core.Skills.Roles.RoleException"/> for an unknown personality, and <see cref="SessionLlmException"/> for an
    /// LLM profile this instance doesn't offer (<paramref name="llm"/>, F30; null: its own model), before anything is created.
    /// A minion (depth <see cref="MinionDepth"/>, F30) names its one <paramref name="tool"/>, which must be its parent
    /// session's (<paramref name="tags"/>' parent, found here: minions run on their specialist's own instance) and never
    /// one of <see cref="MinionTools.NeverHandedDown"/>; its toolsets, when none are sent, are the one that builds the
    /// tool. Every other depth refuses a tool, and only depth 1 loads <see cref="MinionsToolset"/>.
    /// </summary>
    public AgentSession Create(IEnumerable<string>? toolsets = null, UsageTags? tags = null, int depth = 0, string? personality = null,
        UserPlace? place = null, string? llm = null, string? tool = null)
    {
        if (depth is < 0 or > MaxDepth)
            throw new ToolsetException(MessageText.Format(ErrorMessage.SessionDepthInvalid, new { Max = MaxDepth }));
        if (string.IsNullOrWhiteSpace(tool)) tool = null;
        if (depth == MinionDepth) CheckMinion(tags, tool);
        else if (tool is not null)
            throw new ToolsetException(MessageText.Format(ErrorMessage.ToolOnlyForMinions, new { MinionDepth, Depth = depth }));
        // A minion names its tool; the toolset that builds it is implied (StartMinion sends none).
        var names = ToolsetCatalog.Normalise(depth == MinionDepth && toolsets is null ? [ToolsetCatalog.ToolsetOf(tool!)
                ?? throw new ToolsetException(MessageText.Format(ErrorMessage.NoToolsetForTool, new { Tool = tool }))]
            : toolsets ?? DefaultToolsets);
        // A role's own toolsets (F33: the cook's `shops?`, from its file's header) come with it, beside what its parent
        // passed; then every name, optional or not, is held to the same rules.
        names = ToolsetCatalog.Normalise(names.Concat(RoleCatalog.ToolsetsOf(personality, (RoleLevel)depth)));
        var plain = names.Select(ToolsetCatalog.NameOf).ToList();
        if (depth > 0 && plain.Contains(SpecialistsToolset))
            throw new ToolsetException(MessageText.Format(ErrorMessage.SubSessionCannotDelegate, new { Toolset = SpecialistsToolset }));
        if (depth == MinionDepth && plain.Contains(MinionsToolset))
            throw new ToolsetException(MessageText.Format(ErrorMessage.SubSessionCannotDelegate, new { Toolset = MinionsToolset }));
        if (depth == 0 && plain.Contains(MinionsToolset))
            throw new ToolsetException(MessageText.Format(ErrorMessage.MinionsOnlyForSpecialists));
        if (string.IsNullOrEmpty(personality)) personality = null;
        // A sub-session's role is named by its parent (CL10); with none it gets the default mode's voice, no routing.
        if (string.IsNullOrWhiteSpace(llm)) llm = null;
        // What the session loads: an optional toolset this instance lacks is left out, and the session says so.
        names = ToolsetCatalog.Resolve(names, engines.Context).Select(t => t.Name).ToList();
        var engine = engines.GetOrCreate(names, personality, (RoleLevel)depth, llm, tool);

        DropIdle(DateTimeOffset.UtcNow);

        var session = new AgentSession($"sess_{Guid.NewGuid():N}", engine, names, depth, tags ?? UsageTags.None, personality, place, llm, tool);
        _sessions[session.Id] = session;
        return session;
    }

    /// <summary>
    /// A minion's checks (F30), in code and before any HTTP or engine: capability delegation means it can never do more
    /// than the specialist that sent it, which hands it one tool of its own (Louis: no list per role; the specialist's
    /// toolbox is the only one). The role itself (a minion role, not a specialist's or the base) is the factory's check,
    /// as for every personality.
    /// </summary>
    private void CheckMinion(UsageTags? tags, string? tool)
    {
        if (tool is null)
            throw new ToolsetException(MessageText.Format(ErrorMessage.MinionNeedsTool, new { Depth = MinionDepth }));
        if (string.IsNullOrWhiteSpace(tags?.Parent))
            throw new ToolsetException(MessageText.Format(ErrorMessage.MinionNeedsParent, new { Example = "sess_…#3" }));
        string parentId = tags.Parent.Split('#')[0];
        if (Get(parentId) is not { } parent)
            throw new ToolsetException(MessageText.Format(ErrorMessage.MinionParentUnknown, new { Session = parentId }));
        if (MinionTools.NeverHandedDown.Contains(tool))
            throw new ToolsetException(MessageText.Format(ErrorMessage.ToolNeverHandedDown, new { Tool = tool }));
        if (!parent.Engine.Tools.Any(t => string.Equals(t.Name, tool, StringComparison.Ordinal)))
            throw new ToolsetException(MessageText.Format(ErrorMessage.ParentLacksTool, new { Tool = tool }));
    }

    public AgentSession? Get(string id) => _sessions.GetValueOrDefault(id);

    public bool Remove(string id)
    {
        if (!_sessions.TryRemove(id, out var session)) return false;
        Forget(session);
        return true;
    }

    /// <summary>Drops every session idle at <paramref name="now"/> (F26: with the sub-sessions it kept); returns how many.</summary>
    internal int DropIdle(DateTimeOffset now)
    {
        int dropped = 0;
        foreach (var idle in _sessions.Values.Where(s => !s.IsBusy && now - s.LastActivity > IdleTimeout))
        {
            if (!_sessions.TryRemove(idle.Id, out var session)) continue;
            Forget(session);
            dropped++;
        }
        return dropped;
    }

    /// <summary>Stops the session's turn and closes its kept sub-sessions on the peer; the closing never blocks the caller.</summary>
    private static void Forget(AgentSession session)
    {
        session.Cancel();
        _ = session.SubSessions.CloseAllAsync();
    }
}
