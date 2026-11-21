namespace LouisAgent.Core.Pricing.Prices;

/// <summary>One model's prices, optionally with a long-prompt tier (Claude Haiku 5.5: 5× above 100,000 tokens).</summary>
public sealed class ModelPrice : TokenPrices
{
    public LongPromptPrices? LongPrompt { get; init; }
}
