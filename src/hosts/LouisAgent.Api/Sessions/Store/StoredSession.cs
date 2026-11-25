using LouisAgent.Api.Sessions.Opening;

namespace LouisAgent.Api.Sessions.Store;

/// <summary>
/// How a chat opened, as the store keeps it (F43.1): its id, the request it opened with, and when. A resumed chat is
/// opened again from this request, the same way it was opened the first time.
/// </summary>
/// <param name="Id">The chat's id, <c>sess_…</c>.</param>
/// <param name="Opening">
/// What the chat was asked to open with: its toolsets, identity, role, place and the rest.
/// </param>
/// <param name="OpenedAt">When it opened, in UTC.</param>
/// <param name="SystemPrompt">
/// The system prompt it opened with, which a resumed chat sends again: today's may have changed since, and the model
/// checks its earlier thinking against the one it was sent.
/// </param>
/// <param name="AdmittedAs">
/// The identity the open door opened it as, for a visitor's chat, so the door lets it in again after a restart; none
/// for a chat opened with the key, which still needs the key.
/// </param>
internal sealed record StoredSession(
    string Id,
    SessionRequest Opening,
    DateTimeOffset OpenedAt,
    string SystemPrompt,
    string? AdmittedAs = null);
