using LouisAgent.Puls.Admin.Reviews;

namespace LouisAgent.Puls.Admin.Notes;

/// <summary>
/// A note as the page holds it: what was written, when, and which conversation it's on, if any.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
public sealed record RunNoteEntry(string Text, DateTimeOffset At, ConversationRef? Conversation);
