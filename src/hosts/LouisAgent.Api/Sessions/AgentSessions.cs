using LouisAgent.Api.Endpoints.Routing;
using LouisAgent.Api.Sessions.Opening;
using LouisAgent.Api.Sessions.Store;
using LouisAgent.SessionStream.Wire.Requests;
using LouisAgent.Choices;
using LouisAgent.Core.Agent.Toolsets;
using System.Collections.Concurrent;
using LouisAgent.Core.Hosting;

namespace LouisAgent.Api.Sessions;

/// <summary>
/// The sessions this instance is serving, in memory, each written through a store so a restart doesn't end it
/// (F43.1); idle ones are dropped from memory so it doesn't grow forever. Each session gets the engine of the toolsets
/// it asks for (F5), from <see cref="AgentEngineFactory"/>.
/// </summary>
internal sealed class AgentSessions(AgentEngineFactory engines, ISessionStore? store = null)
{
    /// <summary>
    /// Where the sessions are kept beyond this process: the host's, or one in memory when none is given.
    /// </summary>
    private readonly ISessionStore _store = store ?? new InMemorySessionStore();

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
    /// keeps it, in memory and in the store (F43.1); idle sessions are dropped from memory first, so it doesn't grow.
    /// An eval trial's session is never stored: it replays from its recordings and has no next day.
    /// </summary>
    /// <param name="request">What the session is asked to open with.</param>
    /// <param name="admittedAs">The identity the open door opened it as, for a visitor; none with the key.</param>
    /// <param name="cancellationToken">Stops the store's write.</param>
    public async Task<AgentSession> CreateAsync(SessionRequest request, string? admittedAs = null,
        CancellationToken cancellationToken = default)
    {
        var opening = new SessionOpening(engines, request, Get);
        var session = opening.Open();

        DropIdle(DateTimeOffset.UtcNow);
        _sessions[session.Id] = session;
        if (request.Replay is null)
        {
            var stored = new StoredSession(session.Id, request, session.CreatedAt, admittedAs);
            await _store.SaveOpeningAsync(stored, cancellationToken);
        }

        return session;
    }

    /// <summary>The session by its id, if this instance holds it in memory.</summary>
    public AgentSession? Get(string id) => _sessions.GetValueOrDefault(id);

    /// <summary>
    /// The session by its id: from memory, or read back from the store after a restart or an idle drop (F43.1), opened
    /// again from the request it opened with, under the same id, its turns restored; none when neither has it.
    /// </summary>
    public async Task<AgentSession?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        if (Get(id) is { } held) return held;
        if (await _store.LoadAsync(id, cancellationToken) is not { } saved) return null;

        var session = new SessionOpening(engines, saved.Session.Opening, Get).Open(saved.Session.Id);
        session.Restore(saved.Turns);
        // Two messages for the same chat at once resume it once: the first one kept is the one both get.
        return _sessions.GetOrAdd(session.Id, session);
    }

    /// <summary>
    /// Whether this instance has the chat, in memory or in its store, and the identity a visitor's chat was admitted
    /// as, for the open door (F43.1).
    /// </summary>
    public async Task<SessionHolding> HoldingAsync(string id, CancellationToken cancellationToken = default)
    {
        if (await _store.LoadAsync(id, cancellationToken) is { } saved)
            return new SessionHolding(true, saved.Session.AdmittedAs);
        return new SessionHolding(Get(id) is not null, null);
    }

    /// <summary>
    /// Ends a session and the sub-sessions it kept, and forgets it in the store; false when there was none by that id.
    /// </summary>
    public async Task<bool> RemoveAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!_sessions.TryRemove(id, out var session)) return false;
        Forget(session);
        await _store.DeleteAsync(id, cancellationToken);
        return true;
    }

    /// <summary>
    /// How a turn of this session is saved when it ends (F43.1): from the history as it stands now, with the message's
    /// text attachments; none for an eval trial's session, which is never stored.
    /// </summary>
    public TurnSave? TurnSaveFor(AgentSession session, SendMessageRequest request)
    {
        if (session.Replay is not null) return null;
        var text = (request.Attachments ?? []).Where(attachment => attachment.Bytes is null)
            .ToDictionary(attachment => attachment.Name, attachment => attachment.Content ?? string.Empty);
        return new TurnSave(_store, session.History.Count, text);
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
