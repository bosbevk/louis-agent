using Microsoft.Extensions.AI;

namespace LouisAgent.Api.Sessions.Store;

/// <summary>
/// One finished turn of a chat, as the store keeps it (F43.1): the messages it added to the history, whole, thinking
/// included (ADR 0008), and the text attachments sent with it. Appended, never rewritten.
/// </summary>
/// <param name="Number">The turn's number in its chat, from 1.</param>
/// <param name="At">When it finished, in UTC: the chat's last activity once it is the newest.</param>
/// <param name="Messages">The messages the turn added to the history, in order.</param>
/// <param name="Attachments">The text attachments sent with its message, by name; photos and PDFs never.</param>
internal sealed record StoredTurn(
    int Number,
    DateTimeOffset At,
    IReadOnlyList<ChatMessage> Messages,
    IReadOnlyDictionary<string, string> Attachments);
