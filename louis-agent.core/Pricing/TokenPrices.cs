namespace LouisAgent.Core.Pricing;

using System.Text.Json.Serialization;

/// <summary>Prices for one kind of token use, in the table's currency per million tokens.</summary>
public class TokenPrices
{
    [JsonRequired] public decimal Input { get; init; }
    [JsonRequired] public decimal Output { get; init; }
    [JsonRequired] public decimal CacheRead { get; init; }
    [JsonRequired, JsonPropertyName("cache_write_5m")] public decimal CacheWrite5m { get; init; }
    [JsonRequired, JsonPropertyName("cache_write_1h")] public decimal CacheWrite1h { get; init; }

    internal IEnumerable<(string Name, decimal Value)> All() =>
    [
        ("input", Input), ("output", Output), ("cache_read", CacheRead), ("cache_write_5m", CacheWrite5m),
        ("cache_write_1h", CacheWrite1h),
    ];
}
