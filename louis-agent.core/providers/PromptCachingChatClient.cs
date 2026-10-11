namespace louis_agent.core.providers;

using louis_agent.core.usage;
using Microsoft.Extensions.AI;

/// <summary>
/// Marks each request for the provider's prompt cache (F4), through the provider's <see cref="IPromptCachingStrategy"/>;
/// provider-neutral itself. Sits inside the tool loop, so every round is marked, not only a turn's first request.
/// Requests in a <see cref="UsagePurpose.Summary"/> scope go out unmarked: a summary is never repeated, so caching it
/// would only pay the write premium.
/// </summary>
public sealed class PromptCachingChatClient(IChatClient inner, IPromptCachingStrategy strategy) : DelegatingChatClient(inner)
{
    public IPromptCachingStrategy Strategy => strategy;

    public override Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var (prepared, preparedOptions) = Prepare(messages, options);
        return base.GetResponseAsync(prepared, preparedOptions, cancellationToken);
    }

    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var (prepared, preparedOptions) = Prepare(messages, options);
        return base.GetStreamingResponseAsync(prepared, preparedOptions, cancellationToken);
    }

    private (IEnumerable<ChatMessage> Messages, ChatOptions? Options) Prepare(IEnumerable<ChatMessage> messages, ChatOptions? options) =>
        UsageScope.Current?.Purpose == UsagePurpose.Summary ? (messages, options) : strategy.Prepare(messages, options);
}