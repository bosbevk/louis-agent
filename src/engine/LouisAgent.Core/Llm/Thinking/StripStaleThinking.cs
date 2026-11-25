using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Llm.Thinking;

/// <summary>
/// The default: takes the thinking out of a resumed chat's saved turns, and keeps their words, tool calls and
/// results. The model loses what it thought then, but every request is valid on any provider.
/// </summary>
public sealed class StripStaleThinking : IStaleThinking
{
    /// <summary>The one there is; it holds nothing.</summary>
    public static readonly StripStaleThinking Instance = new();

    /// <summary>Each saved message without its thinking; one that held nothing else is left out.</summary>
    public IReadOnlyList<ChatMessage> Restore(IReadOnlyList<ChatMessage> saved) =>
        [.. saved.Select(WithoutThinking).OfType<ChatMessage>()];

    /// <summary>The message without its thinking, or none when thinking was all it held.</summary>
    private static ChatMessage? WithoutThinking(ChatMessage message)
    {
        var kept = message.Contents.Where(content => content is not TextReasoningContent).ToList();
        if (kept.Count == 0) return null;
        return new ChatMessage(message.Role, kept)
        {
            AuthorName = message.AuthorName,
            MessageId = message.MessageId,
            AdditionalProperties = message.AdditionalProperties
        };
    }
}
