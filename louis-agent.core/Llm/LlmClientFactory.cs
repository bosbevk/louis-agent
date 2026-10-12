namespace LouisAgent.Core.Llm;

using System.ClientModel;
using Anthropic;
using Anthropic.Core;
using Anthropic.Models.Messages;
using LouisAgent.Core.Config;
using Microsoft.Extensions.AI;
using OllamaSharp;
using OpenAI;
using LouisAgent.Core.Llm.AnthropicProvider;
using LouisAgent.Core.Messages;

/// <summary>Maps <see cref="LlmOptions.Provider"/> to an <see cref="IChatClient"/>. Add a provider by adding one entry.</summary>
public sealed class LlmClientFactory : ILlmClientFactory
{
    /// <summary>Output limit per reply, thinking included: room for a sizeable file in one tool call; larger files are
    /// written in chunks (see default.md). The engine asks for it on every request.</summary>
    internal const int MaxOutputTokens = 16_000;

    private readonly Dictionary<string, Func<LlmOptions, IChatClient>> _providers =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [LlmOptions.Anthropic] = CreateAnthropic,
            [LlmOptions.Ollama] = CreateOllama,
            [LlmOptions.OpenAiCompatible] = CreateOpenAiCompatible,
        };

    public IChatClient Create(LlmOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Model))
            throw new ArgumentException(MessageText.Format(ErrorMessage.LlmModelRequired), nameof(options));

        if (!_providers.TryGetValue(options.Provider ?? string.Empty, out var create))
            throw new ArgumentException(
                MessageText.Format(ErrorMessage.UnknownProvider, new { options.Provider, Supported = string.Join(", ", _providers.Keys) }),
                nameof(options));

        // Innermost in the engine's pipeline, under the tool loop, so every round is marked.
        return new PromptCachingChatClient(create(options), CreatePromptCaching(options));
    }

    /// <summary>Anthropic reports cache writes outside the standard fields; every other provider follows the contract.</summary>
    public IUsageMapper CreateUsageMapper(LlmOptions options) =>
        string.Equals(options.Provider, LlmOptions.Anthropic, StringComparison.OrdinalIgnoreCase)
            ? new AnthropicUsageMapper()
            : new StandardUsageMapper();

    /// <summary>
    /// Anthropic's markers when <c>LLM_CACHE</c> is on; every other provider sends requests as they are. Both settings are
    /// read for every provider, so a mistyped value fails at start-up wherever it is set.
    /// </summary>
    public IPromptCachingStrategy CreatePromptCaching(LlmOptions options)
    {
        bool cache = options.ResolveCache();
        CacheLifetime prefixLifetime = options.ResolveCacheTtl();
        return cache && string.Equals(options.Provider, LlmOptions.Anthropic, StringComparison.OrdinalIgnoreCase)
            ? new AnthropicPromptCaching(prefixLifetime, options.Model, MaxOutputTokens)
            : NoPromptCaching.Instance;
    }

    private static IChatClient CreateAnthropic(LlmOptions o)
    {
        if (string.IsNullOrWhiteSpace(o.ApiKey))
            throw new InvalidOperationException(MessageText.Format(ErrorMessage.AnthropicKeyRequired));

        var clientOptions = new ClientOptions { ApiKey = o.ApiKey };
        if (!string.IsNullOrWhiteSpace(o.Endpoint)) clientOptions.BaseUrl = o.Endpoint;
        // Keys not scoped to a workspace are rejected unless the request names one.
        if (!string.IsNullOrWhiteSpace(o.WorkspaceId))
            clientOptions.ExtraHeaders = new Dictionary<string, string> { ["anthropic-workspace-id"] = o.WorkspaceId };
        IChatClient client = new AnthropicClient(clientOptions).AsIChatClient(o.Model, MaxOutputTokens);
        return UsesThinkingBudget(o.Model)
            ? new ChatClientBuilder(client).Use(
                (messages, options, next, ct) => next.GetResponseAsync(messages, WithThinkingBudget(options, o.Model), ct),
                (messages, options, next, ct) => next.GetStreamingResponseAsync(messages, WithThinkingBudget(options, o.Model), ct)).Build()
            : client;
    }

    // Claude models before 4.6 reject adaptive thinking (which the adapter sends for ChatOptions.Reasoning)
    // and take a fixed thinking budget instead.
    private static readonly string[] ThinkingBudgetModelPrefixes =
    [
        "claude-haiku-4-5", "claude-sonnet-4-5", "claude-opus-4-5", "claude-opus-4-1",
        "claude-sonnet-4-0", "claude-opus-4-0", "claude-sonnet-4-2", "claude-opus-4-2", "claude-3",
    ];

    internal static bool UsesThinkingBudget(string model) =>
        ThinkingBudgetModelPrefixes.Any(p => model.StartsWith(p, StringComparison.OrdinalIgnoreCase));

    /// <summary>Thinking tokens count toward max_tokens, so the budget leaves room for the answer and tool calls.</summary>
    internal static long ThinkingBudget(ReasoningEffort effort) => effort switch
    {
        ReasoningEffort.Low => 2_048,
        ReasoningEffort.High => 8_192,
        _ => 4_096,
    };

    /// <summary>Swaps ChatOptions.Reasoning for an explicit budget_tokens thinking config.</summary>
    internal static ChatOptions? WithThinkingBudget(ChatOptions? options, string model)
    {
        if (options?.Reasoning?.Effort is not { } effort || effort == ReasoningEffort.None) return options;

        options = options.Clone();
        options.Reasoning = null;
        var previousFactory = options.RawRepresentationFactory;
        options.RawRepresentationFactory = client =>
        {
            // The adapter keeps model and max_tokens from this object, then adds the messages, tools and options.
            var raw = previousFactory?.Invoke(client) as MessageCreateParams ?? new MessageCreateParams
            {
                Model = model, MaxTokens = options.MaxOutputTokens ?? MaxOutputTokens, Messages = [],
            };
            return raw with { Thinking = new ThinkingConfigEnabled { BudgetTokens = ThinkingBudget(effort) } };
        };
        return options;
    }


    /// <summary>A local model on a small GPU writes ~5 tokens a second, so one reply can take minutes; HttpClient's
    /// default 100 seconds cancelled replies Ollama was still writing.</summary>
    internal static readonly TimeSpan OllamaTimeout = TimeSpan.FromMinutes(10);

    private static IChatClient CreateOllama(LlmOptions o) => new OllamaApiClient(CreateOllamaHttpClient(o), o.Model);

    internal static HttpClient CreateOllamaHttpClient(LlmOptions o) => new()
    {
        BaseAddress = new Uri(string.IsNullOrWhiteSpace(o.Endpoint) ? "http://localhost:11434" : o.Endpoint),
        Timeout = OllamaTimeout,
    };

    private static IChatClient CreateOpenAiCompatible(LlmOptions o)
    {
        if (string.IsNullOrWhiteSpace(o.Endpoint))
            throw new InvalidOperationException(MessageText.Format(ErrorMessage.OpenAiEndpointRequired));

        // Local servers (LM Studio, vLLM, ...) often need no key, but the SDK requires a non-empty credential.
        var client = new OpenAIClient(
            new ApiKeyCredential(string.IsNullOrWhiteSpace(o.ApiKey) ? "none" : o.ApiKey),
            new OpenAIClientOptions { Endpoint = new Uri(o.Endpoint) });
        return client.GetChatClient(o.Model).AsIChatClient();
    }
}
