namespace LouisAgent.Core.Llm.Caching;

using Microsoft.Extensions.AI;

/// <summary>
/// Sends requests as they are: for Ollama, for OpenAI-compatible endpoints (OpenAI caches long prefixes on its own and
/// reports the cached tokens, which <c>StandardUsageMapper</c> reads), and for <c>LLM_CACHE=off</c>.
/// </summary>
public sealed class NoPromptCaching : IPromptCachingStrategy
{
    public static readonly NoPromptCaching Instance = new();

    public (IEnumerable<ChatMessage> Messages, ChatOptions? Options) Prepare(IEnumerable<ChatMessage> messages, ChatOptions? options) =>
        (messages, options);
}