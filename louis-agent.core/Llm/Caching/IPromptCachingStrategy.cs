namespace LouisAgent.Core.Llm.Caching;

using Microsoft.Extensions.AI;

/// <summary>
/// How one provider marks a request for its prompt cache (F4). <see cref="PromptCachingChatClient"/> asks it for every
/// request; <see cref="LlmClientFactory"/> picks it once per provider, as it picks the usage mapper.
/// </summary>
public interface IPromptCachingStrategy
{
    /// <summary>
    /// The messages and options to send instead of the caller's. Never changes the caller's list, messages or options:
    /// they are the conversation's history, which is append-only (docs/CLAUDE.md, *Key design decisions*).
    /// </summary>
    (IEnumerable<ChatMessage> Messages, ChatOptions? Options) Prepare(IEnumerable<ChatMessage> messages, ChatOptions? options);
}