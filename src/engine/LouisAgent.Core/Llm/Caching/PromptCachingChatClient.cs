using LouisAgent.Core.Usage.Records;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Llm.Caching;

/// <summary>
/// Marks each request for the provider's prompt cache (F4), through the provider's
/// <see cref="IPromptCachingStrategy"/>; provider-neutral itself. Sits inside the tool loop, so every round is marked,
/// not only a turn's first request. Requests in a <see cref="UsagePurpose.Summary"/> or a
/// <see cref="UsagePurpose.Tool"/> scope go out unmarked: neither is repeated, so caching it would only pay the write
/// premium (F171.1).
/// </summary>
public sealed class PromptCachingChatClient(IChatClient inner, IPromptCachingStrategy strategy)
    : DelegatingChatClient(inner)
{
    /// <summary>How this provider marks a request, for a test or a host that wants to see it.</summary>
    public IPromptCachingStrategy Strategy => strategy;

    /// <summary>Sends the request marked, unless it is one that won't be repeated.</summary>
    public override Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var (prepared, preparedOptions) = Prepare(messages, options);
        return base.GetResponseAsync(prepared, preparedOptions, cancellationToken);
    }

    /// <summary>Streams the request marked, unless it is one that won't be repeated.</summary>
    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var (prepared, preparedOptions) = Prepare(messages, options);
        return base.GetStreamingResponseAsync(prepared, preparedOptions, cancellationToken);
    }

    /// <summary>The request as the strategy marks it, or as it came for a summary or a tool's request.</summary>
    private (IEnumerable<ChatMessage> Messages, ChatOptions? Options) Prepare(IEnumerable<ChatMessage> messages,
        ChatOptions? options) =>
        UsageScope.Current?.Purpose is UsagePurpose.Summary or UsagePurpose.Tool
            ? (messages, options)
            : strategy.Prepare(messages, options);
}
