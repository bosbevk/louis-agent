using System.Text.Json.Serialization;

namespace LouisAgent.Evals.Protocol.Parts.Conversations;

/// <summary>
/// Which turn a line is about: the mode, the repeat, the task, and the turn's number in the conversation.
/// </summary>
/// <remarks>
/// A value used as a key (ADR 0040): two keys with the same four values are the same turn, so a reader can group a
/// turn's lines with it. The events keep the four fields flat on the wire and hand this out as their <c>Key</c>.
/// </remarks>
public sealed record TurnKey(string Mode, int Repeat, string Task, int Turn)
{
    /// <summary>The conversation this turn is part of.</summary>
    [JsonIgnore]
    public ConversationKey Conversation => new(Mode, Repeat, Task);
}
