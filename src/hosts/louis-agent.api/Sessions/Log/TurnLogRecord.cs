namespace LouisAgent.Api.Sessions.Log;

/// <summary>
/// One finished turn, as the admin's overview reads it (F127.8): when, which session and turn, who answered, the page
/// and the part of the site it was asked from, and the names of the tools it called. Never what anyone said.
/// </summary>
/// <param name="At">When the turn ended, in UTC (ADR 0046).</param>
/// <param name="Session">The session's id, which the ledger's records carry too.</param>
/// <param name="Turn">The turn's number in its session, from one.</param>
/// <param name="Identity">Who answered, by the identity's name; none for a session opened as none.</param>
/// <param name="Page">The page's address the message came from; none when it sent no page.</param>
/// <param name="Focus">The part of the site that page is (F127.7); none when the site names none.</param>
/// <param name="Tools">The names of the tools the turn called, in order, each once.</param>
public sealed record TurnLogRecord(
    DateTimeOffset At,
    string Session,
    int Turn,
    string? Identity,
    string? Page,
    string? Focus,
    IReadOnlyList<string> Tools);
