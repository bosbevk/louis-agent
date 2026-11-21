namespace LouisAgent.Core.Pricing.Prices;

using System.Text.Json.Serialization;

/// <summary>Higher prices for a request whose prompt (input, cache reads and writes) is over <see cref="AboveTokens"/>.</summary>
public sealed class LongPromptPrices : TokenPrices
{
    [JsonRequired] public int AboveTokens { get; init; }
}
