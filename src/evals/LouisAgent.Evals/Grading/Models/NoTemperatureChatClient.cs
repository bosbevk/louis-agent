using Microsoft.Extensions.AI;

namespace LouisAgent.Evals.Grading.Models;

/// <summary>
/// Takes the temperature off a request before it reaches a provider that refuses one. Anthropic answers a judge's
/// temperature 0 with "`temperature` is deprecated for this model", so a paid judge couldn't run at all.
/// </summary>
/// <remarks>
/// Decorator: the judges keep asking for temperature 0, which a local model honours and which makes its re-scores
/// repeat exactly, and only the client <see cref="JudgeClients"/> builds for that provider drops it. Without this each
/// judge would have to know which provider it runs on, which ADR 0003 keeps in one place. The options are cloned,
/// because they belong to the caller.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0003-provider-strategies.md">ADR 0003: provider strategies</seealso>
internal sealed class NoTemperatureChatClient(IChatClient inner) : DelegatingChatClient(inner)
{
    /// <inheritdoc />
    public override Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages,
        ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        base.GetResponseAsync(messages, Without(options), cancellationToken);

    /// <inheritdoc />
    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
        ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        base.GetStreamingResponseAsync(messages, Without(options), cancellationToken);

    /// <summary>A copy of the options with no temperature.</summary>
    private static ChatOptions? Without(ChatOptions? options)
    {
        if (options is null) return null;

        var copy = options.Clone();
        copy.Temperature = null;
        return copy;
    }
}
