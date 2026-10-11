using System.Collections.Concurrent;
using louis_agent.core.tools;
using louis_agent.core.usage;
using Microsoft.Extensions.AI;

namespace LouisAgent.Api;

/// <summary>One conversation: its history and the turn in progress, if any.</summary>
internal sealed class AgentSession(string id, List<ChatMessage> history, UsageTags tags, DisplayCurrency? display = null)
{
    private readonly SemaphoreSlim _turnLock = new(1, 1);
    private CancellationTokenSource? _activeTurn;

    public string Id { get; } = id;
    public List<ChatMessage> History { get; } = history;

    /// <summary>What the caller said the session is for (e.g. the orchestrator's fix and run); on every usage record.</summary>
    public UsageTags Tags { get; } = tags;
    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastActivity { get; private set; } = DateTimeOffset.UtcNow;
    public bool IsBusy => _turnLock.CurrentCount == 0;

    /// <summary>Messages started in this session; the current one's number is the usage ledger's turn.</summary>
    public int Turns { get; private set; }

    /// <summary>The usage of the session's finished turns, for /usage and GET /sessions/{id} (F3), in the display currency (F14).</summary>
    public UsageSession Usage { get; } = new(display);

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

/// <summary>In-memory sessions (like the ACP server); idle ones are dropped so memory doesn't grow forever.</summary>
internal sealed class AgentSessions(AgentEngine engine)
{
    private static readonly TimeSpan IdleTimeout = TimeSpan.FromHours(4);
    private readonly ConcurrentDictionary<string, AgentSession> _sessions = new();

    public AgentSession Create(UsageTags? tags = null)
    {
        foreach (var idle in _sessions.Values.Where(s => !s.IsBusy && DateTimeOffset.UtcNow - s.LastActivity > IdleTimeout))
            _sessions.TryRemove(idle.Id, out _);

        var session = new AgentSession($"sess_{Guid.NewGuid():N}", engine.NewHistory(), tags ?? UsageTags.None, engine.Display);
        _sessions[session.Id] = session;
        return session;
    }

    public AgentSession? Get(string id) => _sessions.GetValueOrDefault(id);

    public bool Remove(string id)
    {
        if (!_sessions.TryRemove(id, out var session)) return false;
        session.Cancel();
        return true;
    }
}
