namespace LouisAgent.Api.Sessions.Store;

/// <summary>A chat read back from the store (F43.1): how it opened, and its turns in order.</summary>
/// <param name="Session">How it opened.</param>
/// <param name="Turns">Its finished turns, oldest first.</param>
internal sealed record SavedSession(StoredSession Session, IReadOnlyList<StoredTurn> Turns)
{
    /// <summary>When the chat was last used: its newest turn, or its opening when it has none.</summary>
    public DateTimeOffset LastActivity => Turns.Count == 0 ? Session.OpenedAt : Turns[^1].At;

    /// <summary>
    /// The number of its newest kept turn, or 0 when it has none. A turn that kept nothing, such as a command, still
    /// took its number, so this is the count to go on from, not how many turns are kept.
    /// </summary>
    public int LastTurnNumber => Turns.Count == 0 ? 0 : Turns.Max(turn => turn.Number);
}
