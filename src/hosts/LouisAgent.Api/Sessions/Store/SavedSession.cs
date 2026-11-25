namespace LouisAgent.Api.Sessions.Store;

/// <summary>A chat read back from the store (F43.1): how it opened, and its turns in order.</summary>
/// <param name="Session">How it opened.</param>
/// <param name="Turns">Its finished turns, oldest first.</param>
internal sealed record SavedSession(StoredSession Session, IReadOnlyList<StoredTurn> Turns)
{
    /// <summary>When the chat was last used: its newest turn, or its opening when it has none.</summary>
    public DateTimeOffset LastActivity => Turns.Count == 0 ? Session.OpenedAt : Turns[^1].At;
}
