using LouisAgent.Api.Sessions.Opening;
using LouisAgent.Choices;
using LouisAgent.Core.Agent.Toolsets;
using System.Collections.Concurrent;
using LouisAgent.Core.Hosting;

namespace LouisAgent.Api.Sessions;

/// <summary>
/// In-memory sessions (like the ACP server); idle ones are dropped so memory doesn't grow forever. Each session gets
/// the engine of the toolsets it asks for (F5), from <see cref="AgentEngineFactory"/>.
/// </summary>
internal sealed class AgentSessions(AgentEngineFactory engines)
{
    /// <summary>What a session created without <c>toolsets</c> loads (F5 decision: self-extension only).</summary>
    public static readonly IReadOnlyList<string> DefaultToolsets = [CoreToolset.Skills.Wire()];

    /// <summary>
    /// The toolset that asks another agent (F127.3.1); every session may load it but a lookup agent's, which asks no
    /// one (F127.5).
    /// </summary>
    public static readonly string AgentsToolset = CoreToolset.Agents.Wire();

    /// <summary>
    /// A session nobody has used for this long is dropped at the next create, with the sub-sessions it kept.
    /// </summary>
    public static readonly TimeSpan IdleTimeout = TimeSpan.FromHours(4);

    private readonly ConcurrentDictionary<string, AgentSession> _sessions = new();

    /// <summary>
    /// Opens the session the request asks for (<see cref="SessionOpening"/> holds its steps and its refusals), then
    /// keeps it; idle sessions are dropped first, so memory doesn't grow.
    /// </summary>
    public AgentSession Create(SessionRequest request)
    {
        var opening = new SessionOpening(engines, request, Get);
        var session = opening.Open();

        DropIdle(DateTimeOffset.UtcNow);
        _sessions[session.Id] = session;
        return session;
    }

    /// <summary>The session by its id, if this instance has it.</summary>
    public AgentSession? Get(string id) => _sessions.GetValueOrDefault(id);

    /// <summary>Ends a session and the sub-sessions it kept; false when there was none by that id.</summary>
    public bool Remove(string id)
    {
        if (!_sessions.TryRemove(id, out var session)) return false;
        Forget(session);
        return true;
    }

    /// <summary>
    /// Drops every session idle at <paramref name="now"/> (F26: with the sub-sessions it kept); returns how many.
    /// </summary>
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

    /// <summary>
    /// Stops the session's turn and closes its kept sub-sessions on the peer; the closing never blocks the caller.
    /// </summary>
    private static void Forget(AgentSession session)
    {
        session.Cancel();
        _ = session.SubSessions.CloseAllAsync();
    }
}
