namespace LouisAgent.Api.Sessions.Store;

/// <summary>
/// How one turn is saved when it ends (F43.1): where its messages start in the chat's history, taken before the turn
/// runs, and the text attachments sent with it. Whatever the turn added, a finished one or a failed one, is appended
/// to the store whole, and a turn that added nothing, such as a user's command, appends nothing.
/// </summary>
/// <param name="Store">The store the chat is kept in.</param>
/// <param name="HistoryFrom">The history's length before the turn: its messages are everything after it.</param>
/// <param name="Attachments">The text attachments sent with the turn's message, by name.</param>
internal sealed record TurnSave(ISessionStore Store, int HistoryFrom, IReadOnlyDictionary<string, string> Attachments)
{
    /// <summary>Appends what the turn added to the chat's history, under the turn's number.</summary>
    public async Task AppendAsync(AgentSession session, CancellationToken cancellationToken = default)
    {
        var added = session.History.Skip(HistoryFrom).ToList();
        if (added.Count == 0) return;

        var turn = new StoredTurn(session.Turns, DateTimeOffset.UtcNow, added, Attachments);
        await Store.AppendTurnAsync(session.Id, turn, cancellationToken);
    }
}
