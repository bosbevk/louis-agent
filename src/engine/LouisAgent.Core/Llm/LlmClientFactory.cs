using LouisAgent.Composition.Messages;
using LouisAgent.Core.Config;
using LouisAgent.Core.Llm.Caching;
using LouisAgent.Core.Llm.Messages;
using LouisAgent.Core.Llm.Messages.Holes.Providers;
using LouisAgent.Core.Llm.Providers;
using LouisAgent.Core.Llm.Thinking;
using LouisAgent.Core.Llm.ToolSearch;
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

    /// <summary>The factory over the providers the host gave, found by name without regard to case.</summary>
    public LlmClientFactory(IEnumerable<ILlmProvider> providers) =>
        _providers = providers.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

    /// <summary>The names a host gave, for a message.</summary>
    public IReadOnlyCollection<string> Supported => _providers.Keys;

    /// <summary>
    /// The named provider's client for these settings, wrapped for its prompt cache; a model must be named.
    /// </summary>
    public IChatClient Create(LlmOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Model))
            throw new ArgumentException(MessageText.Format(LlmErrorMessage.LlmModelRequired), nameof(options));

        // Innermost in the engine's pipeline, under the tool loop, so every round is marked.
        return new PromptCachingChatClient(Provider(options).Create(options), CreatePromptCaching(options));
    }

    /// <summary>The provider's own way of counting tokens.</summary>
    public IUsageMapper CreateUsageMapper(LlmOptions options) => Provider(options).CreateUsageMapper();

    /// <summary>The provider's own way with a resumed chat's thinking once its tools moved.</summary>
    public IStaleThinking CreateStaleThinking(LlmOptions options) => Provider(options).CreateStaleThinking(options);

    /// <summary>The provider's own way of letting a chat's tools wait to be found (F97.3).</summary>
    public IToolSearch CreateToolSearch(LlmOptions options) => Provider(options).CreateToolSearch(options);

    /// <summary>
    /// The provider's markers when <c>LLM_CACHE</c> is on; nothing when it's off, whoever the provider is.
    /// </summary>
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

        var unknown = new UnknownProviderHoles(options.Provider, string.Join(", ", Supported));
        throw new ArgumentException(MessageText.Format(LlmErrorMessage.UnknownProvider, unknown), nameof(options));
    }
}
