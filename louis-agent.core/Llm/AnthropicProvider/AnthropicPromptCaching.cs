namespace LouisAgent.Core.Llm.AnthropicProvider;

using LouisAgent.Core.Llm.Caching;
using Anthropic.Models.Messages;
using LouisAgent.Core.Config;
using Microsoft.Extensions.AI;

/// <summary>
/// Anthropic's two cache markers (F4, "How caching works"). The API renders tools, then the system prompt, then the
/// messages, and caches the exact bytes up to each marker:
/// <list type="number">
/// <item>Marker 1, explicit, at the end of the system prompt: tools + system, the prefix every request and every session
/// shares. Its lifetime is <c>LLM_CACHE_TTL</c>, chosen from the measured gaps between requests that share it.</item>
/// <item>Marker 2, automatic (the request's top-level <c>cache_control</c>): the API puts it on the last cacheable
/// block and moves it forward as the conversation grows, so each round reads the round before. Always 5 minutes: only
/// the next round, seconds later, reuses it.</item>
/// </list>
/// Two of the four markers a request may carry; the longer lifetime always comes first, as the API requires.
/// </summary>
public sealed class AnthropicPromptCaching(CacheLifetime prefixLifetime, string model, int maxOutputTokens) : IPromptCachingStrategy
{
    private readonly Ttl _prefixTtl = prefixLifetime == CacheLifetime.OneHour ? Ttl.Ttl1h : Ttl.Ttl5m;

    public CacheLifetime PrefixLifetime => prefixLifetime;

    public (IEnumerable<ChatMessage> Messages, ChatOptions? Options) Prepare(IEnumerable<ChatMessage> messages, ChatOptions? options)
    {
        // A new list: the caller's is the history.
        List<ChatMessage> request = messages.ToList();
        int lastSystem = request.TakeWhile(m => m.Role == ChatRole.System).Count() - 1;
        if (lastSystem >= 0 && request[lastSystem].Contents.LastOrDefault() is TextContent text)
            request[lastSystem] = WithMarker(request[lastSystem], text, _prefixTtl);

        return (request, WithAutomaticCaching(options));
    }

    /// <summary>
    /// A copy of the message whose last block carries the marker. <c>WithCacheControl</c> changes the content it is called
    /// on, so it is called on a new block, never on the history's.
    /// </summary>
    private static ChatMessage WithMarker(ChatMessage message, TextContent last, Ttl ttl)
    {
        var marked = new TextContent(last.Text)
        {
            AdditionalProperties = last.AdditionalProperties?.Clone(),
            Annotations = last.Annotations,
        }.WithCacheControl(ttl);
        ChatMessage copy = message.Clone();
        copy.Contents = [.. message.Contents.Take(message.Contents.Count - 1), marked];
        return copy;
    }

    /// <summary>The top-level <c>cache_control</c>, set through the same hook as the thinking budget, chaining to it.</summary>
    private ChatOptions WithAutomaticCaching(ChatOptions? options)
    {
        ChatOptions prepared = options?.Clone() ?? new ChatOptions();
        var previousFactory = prepared.RawRepresentationFactory;
        prepared.RawRepresentationFactory = client =>
        {
            // The adapter keeps model and max_tokens from this object, then adds the messages, tools and options.
            var raw = previousFactory?.Invoke(client) as MessageCreateParams ?? new MessageCreateParams
            {
                Model = model, MaxTokens = prepared.MaxOutputTokens ?? maxOutputTokens, Messages = [],
            };
            return raw with { CacheControl = new CacheControlEphemeral { Ttl = Ttl.Ttl5m } };
        };
        return prepared;
    }
}