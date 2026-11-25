namespace LouisAgent.Api.Sessions.Store;

/// <summary>
/// Where the API keeps its chats so a restart doesn't end them (F43.1): how a chat opened, then each finished turn,
/// appended. A chat the API no longer holds in memory is read back from here and opened again.
/// </summary>
/// <remarks>
/// Separated Interface with a Strategy per store, the shape Microsoft Agent Framework's message store, LangGraph's
/// checkpointers and OpenAI's Agents SDK sessions share: in memory for tests and a host with no database, MongoDB for
/// production, picked by a setting, so neither a session nor its turn knows which.
/// </remarks>
internal interface ISessionStore
{
    /// <summary>Keeps how a chat opened.</summary>
    Task SaveOpeningAsync(StoredSession session, CancellationToken cancellationToken = default);

    /// <summary>Appends one finished turn to a chat already saved.</summary>
    Task AppendTurnAsync(string sessionId, StoredTurn turn, CancellationToken cancellationToken = default);

    /// <summary>A chat as it was saved, its turns in order, or none when the store doesn't have it.</summary>
    Task<SavedSession?> LoadAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>Forgets a chat and its turns; an id the store doesn't have is no error.</summary>
    Task DeleteAsync(string sessionId, CancellationToken cancellationToken = default);
}
