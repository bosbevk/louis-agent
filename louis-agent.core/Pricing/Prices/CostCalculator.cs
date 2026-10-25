namespace LouisAgent.Core.Pricing.Prices;

using LouisAgent.Core.Usage.Records;


/// <summary>
/// Prices a usage record (Usage spec §4.2): each token kind times its price per million tokens. The kinds don't overlap
/// (F1 subtracts cache reads and writes from the input), so the cost is a plain sum.
/// </summary>
public static class CostCalculator
{
    private const decimal TokensPerPrice = 1_000_000m;

    /// <summary>
    /// The record's cost (0 for a replayed answer), or null when it can't be known: no price for the model (F2-S3: unknown is never free), or the
    /// provider didn't report input or output tokens. Missing cache counts count as 0 (no cache was used).
    /// </summary>
    public static UsageCost? For(UsageRecord record, PriceTable prices)
    {
        // A replayed answer (F18 step 4e) cost nothing: a known 0, even for a model the table doesn't price (ADR 0007).
        if (record.CacheHit == true) return new UsageCost(prices.Currency, 0m, prices.AsOf);
        if (prices.Find(record.Model) is not { } price) return null;
        if (record.Tokens is not { Input: { } input, Output: { } output }) return null;

        long cacheRead = record.Tokens.CacheRead ?? 0;
        long cacheWrite = record.Tokens.CacheWrite ?? 0;
        // F4: the 1-hour part of the writes, when the provider reported the split; the rest are 5-minute writes.
        long cacheWrite1h = record.Tokens.CacheWrite1h ?? 0;

        // A long-prompt tier prices the whole request, cache included, once the prompt is over its threshold.
        long prompt = input + cacheRead + cacheWrite;
        TokenPrices tier = price.LongPrompt is { } longPrompt && prompt > longPrompt.AboveTokens ? longPrompt : price;

        // Each write at its lifetime's price. Reasoning isn't added: it is already part of the output count.
        decimal amount = (input * tier.Input + output * tier.Output + cacheRead * tier.CacheRead +
                          (cacheWrite - cacheWrite1h) * tier.CacheWrite5m + cacheWrite1h * tier.CacheWrite1h) / TokensPerPrice;

        return new UsageCost(prices.Currency, Math.Round(amount, 6, MidpointRounding.AwayFromZero), prices.AsOf);
    }
}