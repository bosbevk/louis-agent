using Anthropic.Models.Messages;
using LouisAgent.Core.Llm.Providers;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Anthropic;

/// <summary>
/// Claude models before 4.6 reject adaptive thinking (which the adapter sends for <c>ChatOptions.Reasoning</c>) and
/// take a fixed <c>budget_tokens</c> instead; this swaps the one for the other on every request to such a model.
/// </summary>
public static class ThinkingBudget
{
    private static readonly string[] ThinkingBudgetModelPrefixes =
    [
        "claude-haiku-4-5", "claude-sonnet-4-5", "claude-opus-4-5", "claude-opus-4-1",
        "claude-sonnet-4-0", "claude-opus-4-0", "claude-sonnet-4-2", "claude-opus-4-2", "claude-3",
    ];

    /// <summary>Whether this model is one of the older ones that takes a budget.</summary>
    public static bool UsesThinkingBudget(string model) =>
        ThinkingBudgetModelPrefixes.Any(p => model.StartsWith(p, StringComparison.OrdinalIgnoreCase));

    /// <summary>Thinking tokens count toward max_tokens, so the budget leaves room for the answer and tool calls.</summary>
    public static long Budget(ReasoningEffort effort) => effort switch
    {
        ReasoningEffort.Low => 2_048,
        ReasoningEffort.High => 8_192,
        _ => 4_096,
    };

    /// <summary>The client with the swap on both the one-shot and the streaming call.</summary>
    public static IChatClient Wrap(IChatClient client, string model) =>
        new ChatClientBuilder(client).Use(
            (messages, options, next, ct) =>
                next.GetResponseAsync(messages, WithThinkingBudget(options, model), ct),
            (messages, options, next, ct) =>
                next.GetStreamingResponseAsync(messages, WithThinkingBudget(options, model), ct)).Build();

    /// <summary>Swaps ChatOptions.Reasoning for an explicit budget_tokens thinking config.</summary>
    public static ChatOptions? WithThinkingBudget(ChatOptions? options, string model)
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
                Model = model,
                MaxTokens = options.MaxOutputTokens ?? LlmLimits.MaxOutputTokens,
                Messages = [],
            };
            return raw with { Thinking = new ThinkingConfigEnabled { BudgetTokens = Budget(effort) } };
        };
        return options;
    }
}
