using LouisAgent.Composition.Messages;
using LouisAgent.Core.Config;
using LouisAgent.Core.Llm.Caching;
using LouisAgent.Core.Llm.Messages;
using LouisAgent.Core.Llm.Providers;
using LouisAgent.Core.Llm.UsageMapping;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Llm;

/// <summary>
/// Finds the provider <c>LLM_PROVIDER</c> names among the ones the host gave and builds the model client through it,
/// wrapped for prompt caching. With no providers it says so: core alone talks to no model.
/// </summary>
/// <remarks>
/// A registry of Strategies (ADR 0003): the providers are <see cref="ILlmProvider"/>s a program composes, each in its
/// own library, so adding one is a reference and a line in the program, and core names no SDK (CL57, ADR 0092).
/// Both cache settings are read here for every provider, so a mistyped value fails at start-up wherever it is set.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0003-provider-strategies.md">ADR 0003: provider strategies</seealso>
public sealed class LlmClientFactory : ILlmClientFactory
{
    private readonly Dictionary<string, ILlmProvider> _providers;

    public LlmClientFactory(IEnumerable<ILlmProvider> providers) =>
        _providers = providers.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

    /// <summary>The names a host gave, for a message.</summary>
    public IReadOnlyCollection<string> Supported => _providers.Keys;

    public IChatClient Create(LlmOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Model))
            throw new ArgumentException(MessageText.Format(LlmErrorMessage.LlmModelRequired), nameof(options));

        // Innermost in the engine's pipeline, under the tool loop, so every round is marked.
        return new PromptCachingChatClient(Provider(options).Create(options), CreatePromptCaching(options));
    }

    /// <summary>The provider's own way of counting tokens.</summary>
    public IUsageMapper CreateUsageMapper(LlmOptions options) => Provider(options).CreateUsageMapper();

    /// <summary>The provider's markers when <c>LLM_CACHE</c> is on; nothing when it's off, whoever the provider is.</summary>
    public IPromptCachingStrategy CreatePromptCaching(LlmOptions options)
    {
        bool cache = options.ResolveCache();
        options.ResolveCacheTtl();
        return cache ? Provider(options).CreatePromptCaching(options) : NoPromptCaching.Instance;
    }

    /// <summary>The provider the settings name, or the error that lists the ones the host gave.</summary>
    private ILlmProvider Provider(LlmOptions options)
    {
        if (_providers.TryGetValue(options.Provider ?? string.Empty, out var provider)) return provider;

        throw new ArgumentException(
            MessageText.Format(LlmErrorMessage.UnknownProvider,
                new
                {
                    options.Provider,
                    Supported = string.Join(", ", Supported)
                }),
            nameof(options));
    }
}
