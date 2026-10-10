namespace louis_agent.core.providers;

using System.ClientModel;
using Anthropic;
using Anthropic.Core;
using Anthropic.Models.Messages;
using louis_agent.core.config;
using louis_agent.core.tools;
using louis_agent.core.usage;
using Microsoft.Extensions.AI;
using OllamaSharp;
using OpenAI;

public interface ILlmClientFactory
{
    IChatClient Create(LlmOptions options);

    /// <summary>How this provider's usage maps to the ledger's token kinds.</summary>
    IUsageMapper CreateUsageMapper(LlmOptions options) => new StandardUsageMapper();
}

/// <summary>Maps <see cref="LlmOptions.Provider"/> to an <see cref="IChatClient"/>. Add a provider by adding one entry.</summary>
public sealed class LlmClientFactory : ILlmClientFactory
{
    private const int DefaultMaxOutputTokens = AgentEngine.MaxOutputTokens;

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
            throw new ArgumentException("LLM_MODEL is required.", nameof(options));

        if (!_providers.TryGetValue(options.Provider ?? string.Empty, out var create))
            throw new ArgumentException(
                $"Unknown LLM_PROVIDER '{options.Provider}'. Supported: {string.Join(", ", _providers.Keys)}.",
                nameof(options));

        return create(options);
    }

    /// <summary>Anthropic reports cache writes outside the standard fields; every other provider follows the contract.</summary>
    public IUsageMapper CreateUsageMapper(LlmOptions options) =>
        string.Equals(options.Provider, LlmOptions.Anthropic, StringComparison.OrdinalIgnoreCase)
            ? new AnthropicUsageMapper()
            : new StandardUsageMapper();

    private static IChatClient CreateAnthropic(LlmOptions o)
    {
        if (string.IsNullOrWhiteSpace(o.ApiKey))
            throw new InvalidOperationException("ANTHROPIC_API_KEY (or LLM_API_KEY) is required for provider 'anthropic'.");

        var clientOptions = new ClientOptions { ApiKey = o.ApiKey };
        if (!string.IsNullOrWhiteSpace(o.Endpoint)) clientOptions.BaseUrl = o.Endpoint;
        // Keys not scoped to a workspace are rejected unless the request names one.
        if (!string.IsNullOrWhiteSpace(o.WorkspaceId))
            clientOptions.ExtraHeaders = new Dictionary<string, string> { ["anthropic-workspace-id"] = o.WorkspaceId };
        IChatClient client = new AnthropicClient(clientOptions).AsIChatClient(o.Model, DefaultMaxOutputTokens);
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
                Model = model, MaxTokens = options.MaxOutputTokens ?? DefaultMaxOutputTokens, Messages = [],
            };
            return raw with { Thinking = new ThinkingConfigEnabled { BudgetTokens = ThinkingBudget(effort) } };
        };
        return options;
    }


    private static IChatClient CreateOllama(LlmOptions o) =>
        new OllamaApiClient(new Uri(string.IsNullOrWhiteSpace(o.Endpoint) ? "http://localhost:11434" : o.Endpoint), o.Model);

    private static IChatClient CreateOpenAiCompatible(LlmOptions o)
    {
        if (string.IsNullOrWhiteSpace(o.Endpoint))
            throw new InvalidOperationException("LLM_ENDPOINT is required for provider 'openai-compatible'.");

        // Local servers (LM Studio, vLLM, ...) often need no key, but the SDK requires a non-empty credential.
        var client = new OpenAIClient(
            new ApiKeyCredential(string.IsNullOrWhiteSpace(o.ApiKey) ? "none" : o.ApiKey),
            new OpenAIClientOptions { Endpoint = new Uri(o.Endpoint) });
        return client.GetChatClient(o.Model).AsIChatClient();
    }
}
