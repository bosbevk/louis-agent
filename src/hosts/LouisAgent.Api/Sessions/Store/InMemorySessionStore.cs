using System.Collections.Concurrent;

namespace LouisAgent.Api.Sessions.Store;

/// <summary>
/// Keeps chats in this process's memory (F43.1): what a test and a host with no database use. A restart forgets them,
/// as every chat was forgotten before the store; MongoDB is the store that outlives one.
/// </summary>
/// <remarks>
/// One of the strategies behind <see cref="ISessionStore" />. A chat's turns are a list it locks while it appends or
/// copies, so a load never sees a turn half added.
/// </remarks>
internal sealed class InMemorySessionStore : ISessionStore
{
    private readonly ConcurrentDictionary<string, (StoredSession Session, List<StoredTurn> Turns)> _chats = new();

    /// <inheritdoc/>
    public Task SaveOpeningAsync(StoredSession session, CancellationToken cancellationToken = default)
    {
        _chats[session.Id] = (session, []);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task AppendTurnAsync(string sessionId, StoredTurn turn, CancellationToken cancellationToken = default)
    {
        if (!_chats.TryGetValue(sessionId, out var chat)) return Task.CompletedTask;
        lock (chat.Turns) chat.Turns.Add(turn);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<SavedSession?> LoadAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (!_chats.TryGetValue(sessionId, out var chat)) return Task.FromResult<SavedSession?>(null);
        lock (chat.Turns) return Task.FromResult<SavedSession?>(new SavedSession(chat.Session, [.. chat.Turns]));
    }

    /// <inheritdoc/>
    public Task DeleteAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        _chats.TryRemove(sessionId, out _);
        return Task.CompletedTask;
    }
}
