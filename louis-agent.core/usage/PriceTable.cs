namespace louis_agent.core.usage;

using System.Text.Json;
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

/// <summary>Higher prices for a request whose prompt (input, cache reads and writes) is over <see cref="AboveTokens"/>.</summary>
public sealed class LongPromptPrices : TokenPrices
{
    [JsonRequired] public int AboveTokens { get; init; }
}

/// <summary>One model's prices, optionally with a long-prompt tier (Claude Haiku 5.5: 5× above 100,000 tokens).</summary>
public sealed class ModelPrice : TokenPrices
{
    public LongPromptPrices? LongPrompt { get; init; }
}

/// <summary>
/// The operator's price table (<c>config/prices.json</c>, Usage spec §4.2): prices per million tokens, matched to a
/// model id by longest prefix, so <c>claude-haiku-4-5</c> also prices <c>claude-haiku-4-5-20251001</c>.
/// </summary>
public sealed class PriceTable
{
    /// <summary>The table's own JSON names (snake_case); comments and trailing commas are allowed in the file.</summary>
    internal static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>The date the prices were copied from the provider; stored on every priced record.</summary>
    public string? AsOf { get; init; }

    public string Currency { get; init; } = "USD";

    /// <summary>Model id prefix → prices.</summary>
    public Dictionary<string, ModelPrice> Models { get; init; } = [];

    /// <summary>A table with no prices: every record stays unpriced (<c>cost: null</c>).</summary>
    public static PriceTable Empty { get; } = new();

    [JsonIgnore]
    public bool IsEmpty => Models.Count == 0;

    /// <summary>
    /// Loads the table at <paramref name="path"/>; a missing file gives <see cref="Empty"/>. A malformed file throws
    /// <see cref="InvalidDataException"/> naming the file, since running with wrong prices is worse than not starting.
    /// </summary>
    public static PriceTable Load(string path)
    {
        if (!File.Exists(path)) return Empty;

        PriceTable? table;
        try
        {
            table = JsonSerializer.Deserialize<PriceTable>(File.ReadAllText(path), Json);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Price table {path} is not valid: {ex.Message}", ex);
        }

        if (table is null) throw new InvalidDataException($"Price table {path} is empty.");
        foreach (var (model, price) in table.Models)
        {
            IEnumerable<(string Name, decimal Value)> prices = price.All().Concat(
                price.LongPrompt?.All().Select(p => (Name: $"long_prompt.{p.Name}", p.Value)) ?? []);
            if (prices.FirstOrDefault(p => p.Value < 0) is { Name: { } negative })
                throw new InvalidDataException($"Price table {path}: '{model}' has a negative {negative} price.");
            if (price.LongPrompt is { AboveTokens: <= 0 })
                throw new InvalidDataException($"Price table {path}: '{model}' long_prompt.above_tokens must be positive.");
        }

        return table;
    }

    /// <summary>The prices for <paramref name="modelId"/>: the entry with the longest matching prefix, or null.</summary>
    public ModelPrice? Find(string? modelId)
    {
        if (string.IsNullOrEmpty(modelId)) return null;
        return Models
            .Where(m => modelId.StartsWith(m.Key, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(m => m.Key.Length)
            .Select(m => m.Value)
            .FirstOrDefault();
    }
}