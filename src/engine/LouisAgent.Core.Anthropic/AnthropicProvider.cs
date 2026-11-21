using Anthropic;
using Anthropic.Core;
using LouisAgent.Composition.Messages;
using LouisAgent.Core.Anthropic.Messages;
using LouisAgent.Core.Config;
using LouisAgent.Core.Llm.Caching;
using LouisAgent.Core.Llm.Providers;
using LouisAgent.Core.Llm.UsageMapping;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Anthropic;

/// <summary>
/// Claude through Anthropic's SDK: the client from the key, endpoint and workspace the settings give, the usage
/// mapper that knows its cache writes, and its prompt-cache markers when caching is on. Older Claude models take a
/// fixed thinking budget instead of adaptive thinking, which <see cref="ThinkingBudget"/> swaps in.
/// </summary>
/// <remarks>
/// The Anthropic Strategy for core's <see cref="ILlmProvider"/> (ADR 0003), in its own library (CL57, ADR 0092) so a
/// host on a local model ships no Anthropic SDK.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0003-provider-strategies.md">ADR 0003: provider strategies</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0092-a-project-holds-what-is-reused-together.md">ADR 0092: a project holds what is reused together</seealso>
public sealed class AnthropicProvider : ILlmProvider
{
    /// <summary>The one instance: it holds nothing of its own.</summary>
    public static readonly AnthropicProvider Instance = new();

    private AnthropicProvider()
    {
    }

    public string Name => LlmOptions.Anthropic;

    /// <summary>The SDK's client as an <see cref="IChatClient"/>, with the thinking budget for models that need one.</summary>
    public IChatClient Create(LlmOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new InvalidOperationException(MessageText.Format(AnthropicErrorMessage.KeyRequired));

        var clientOptions = new ClientOptions { ApiKey = options.ApiKey };
        if (!string.IsNullOrWhiteSpace(options.Endpoint)) clientOptions.BaseUrl = options.Endpoint;
        // Keys not scoped to a workspace are rejected unless the request names one.
        if (!string.IsNullOrWhiteSpace(options.WorkspaceId))
            clientOptions.ExtraHeaders = new Dictionary<string, string>
            {
                ["anthropic-workspace-id"] = options.WorkspaceId
            };

        IChatClient client = new AnthropicClient(clientOptions).AsIChatClient(options.Model, LlmLimits.MaxOutputTokens);
        return ThinkingBudget.UsesThinkingBudget(options.Model) ? ThinkingBudget.Wrap(client, options.Model) : client;
    }

    /// <summary>Anthropic reports cache writes outside the standard fields.</summary>
    public IUsageMapper CreateUsageMapper() => new AnthropicUsageMapper();

    /// <summary>The two markers, the prefix's lifetime from <c>LLM_CACHE_TTL</c>.</summary>
    public IPromptCachingStrategy CreatePromptCaching(LlmOptions options) =>
        new AnthropicPromptCaching(options.ResolveCacheTtl(), options.Model, LlmLimits.MaxOutputTokens);
}
