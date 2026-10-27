namespace LouisAgent.Api.Contracts.Admin;

/// <summary>A note on a run or on one of its conversations: what it says, and when the server kept it.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public sealed record NoteRow(string Text, DateTimeOffset At, ConversationKeyRow? Conversation);

/// <summary>Which conversation of a run: its mode, its repeat and its task.</summary>
public sealed record ConversationKeyRow(string Mode, int Repeat, string Task);

/// <summary>What the portal sends to add a note: the text, and a conversation's key if the note is on one.</summary>
public sealed record NoteRequest(string? Text, ConversationKeyRow? Conversation);
