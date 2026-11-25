using LouisAgent.Core.Config;
using LouisAgent.Core.Llm.Caching;
using LouisAgent.Core.Llm.Thinking;
using LouisAgent.Core.Llm.UsageMapping;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Llm.Providers;

/// <summary>
/// One model provider as the engine sees it (CL57): its name in <c>LLM_PROVIDER</c>, how to build its client from the
/// settings, how it reports usage, how it caches a prompt, and what a resumed chat does with thinking that no longer
/// matches its tools. Each lives in its own library on core
/// (<c>LouisAgent.Core.Anthropic</c>, <c>.core.ollama</c>, <c>.core.openai</c>), and a host hands the ones it wants
/// to <c>HostServices.Providers</c>; core itself ships no SDK.
/// </summary>
/// <remarks>
/// Strategy (Gang of Four), the shape ADR 0003 names: where providers differ, one interface with an implementation
/// per provider, picked by name in <see cref="LlmClientFactory"/>, and nothing else in core branches on a provider's
/// name. The defaults are the contract's: a provider with nothing special to say about usage or caching says nothing.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0003-provider-strategies.md">ADR 0003: provider strategies</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0092-a-project-holds-what-is-reused-together.md">ADR 0092: a project holds what is reused together</seealso>
public interface ILlmProvider
{
    /// <summary>The name <c>LLM_PROVIDER</c> picks it by, compared without case.</summary>
    string Name { get; }

    /// <summary>The SDK's chat client for these settings; refuses settings it can't work with.</summary>
    IChatClient Create(LlmOptions options);

    /// <summary>How this provider reports its token counts; the standard fields unless it says otherwise.</summary>
    IUsageMapper CreateUsageMapper() => new StandardUsageMapper();

    /// <summary>
    /// How a prompt is marked for this provider's cache when caching is on; none unless it has a cache.
    /// </summary>
    IPromptCachingStrategy CreatePromptCaching(LlmOptions options) => NoPromptCaching.Instance;

    /// <summary>
    /// What a chat resumed on moved tools does with its saved thinking; stripping it, which every provider takes,
    /// unless this one can do better.
    /// </summary>
    IStaleThinking CreateStaleThinking(LlmOptions options) => StripStaleThinking.Instance;
}
