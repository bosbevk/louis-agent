using LouisAgent.Core.Config;
using LouisAgent.Core.Llm.Caching;
using LouisAgent.Core.Llm.UsageMapping;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Llm;

/// <summary>
/// Builds the model client the settings ask for, and says how that provider counts tokens and marks its prompt cache.
/// The engine takes this seam; <see cref="LlmClientFactory"/> is the one implementation, over the providers a host
/// hands in (CL57, ADR 0092).
/// </summary>
/// <remarks>
/// Abstract Factory (Gang of Four) with the provider picked by name (ADR 0003): the defaults are the contract's, so a
/// factory with nothing special to say about usage or caching says nothing.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0003-provider-strategies.md">ADR 0003: provider strategies</seealso>
public interface ILlmClientFactory
{
    /// <summary>The chat client for these settings, wrapped for the engine's pipeline; refuses settings it can't work with.</summary>
    IChatClient Create(LlmOptions options);

    /// <summary>How this provider's usage maps to the ledger's token kinds.</summary>
    IUsageMapper CreateUsageMapper(LlmOptions options) => new StandardUsageMapper();

    /// <summary>How this provider marks requests for its prompt cache (F4).</summary>
    IPromptCachingStrategy CreatePromptCaching(LlmOptions options) => NoPromptCaching.Instance;
}
