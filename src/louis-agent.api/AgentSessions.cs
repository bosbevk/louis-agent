using System.Collections.Concurrent;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Skills;
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
    string? personality = null, UserPlace? place = null)
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

    /// <summary>The personality the session chose (F21), or null for personality.md.</summary>
    public string? Personality { get; } = personality;

    /// <summary>Where the user is, when they shared it (F22): set for every turn; the first message says it.</summary>
    public UserPlace? Place { get; } = place;

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

    /// <summary>Sub-sessions are one level deep: a session another agent opened may not open its own.</summary>
    public const int MaxDepth = 1;

    /// <summary>The toolset that opens sub-sessions (step 8); refused at <see cref="MaxDepth"/>.</summary>
    public const string AgentsToolset = "agents";

    /// <summary>A session nobody has used for this long is dropped at the next create, with the sub-sessions it kept.</summary>
    public static readonly TimeSpan IdleTimeout = TimeSpan.FromHours(4);

    private readonly ConcurrentDictionary<string, AgentSession> _sessions = new();

    /// <summary>
    /// Opens a session with <paramref name="toolsets"/> (null: <see cref="DefaultToolsets"/>; empty: none) and
    /// <paramref name="personality"/> (null: personality.md). The session's level is its depth (CL8): its personality comes
    /// from that level's folder, and a sub-session gets no router (default.md): its toolsets were chosen for it and it
    /// can't delegate (F21). Throws <see cref="ToolsetException"/> for an unknown or unavailable
    /// toolset, a depth outside 0..<see cref="MaxDepth"/>, or a sub-session asking for <see cref="AgentsToolset"/>, and
    /// <see cref="Core.Skills.PersonalityException"/> for an unknown personality, before anything is created.
    /// </summary>
    public AgentSession Create(IEnumerable<string>? toolsets = null, UsageTags? tags = null, int depth = 0, string? personality = null,
        UserPlace? place = null)
    {
        if (depth is < 0 or > MaxDepth)
            throw new ToolsetException(MessageText.Format(ErrorMessage.SessionDepthInvalid, new { Max = MaxDepth }));
        var names = ToolsetCatalog.Normalise(toolsets ?? DefaultToolsets);
        if (depth > 0 && names.Contains(AgentsToolset))
            throw new ToolsetException(MessageText.Format(ErrorMessage.SubSessionCannotDelegate, new { Toolset = AgentsToolset }));
        if (string.IsNullOrEmpty(personality)) personality = null;
        // A sub-session its parent gave no role gets one from its toolsets (roles.json): roles in code, not left to the prompt.
        if (depth > 0 && personality is null) personality = engines.RoleFor(names);
        var engine = engines.GetOrCreate(names, personality, (SkillLevel)depth);

        DropIdle(DateTimeOffset.UtcNow);

        var session = new AgentSession($"sess_{Guid.NewGuid():N}", engine, names, depth, tags ?? UsageTags.None, personality, place);
        _sessions[session.Id] = session;
        return session;
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
