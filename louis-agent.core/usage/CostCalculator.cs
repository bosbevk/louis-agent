namespace louis_agent.core.usage;

/// <summary>
/// Prices a usage record (Usage spec §4.2): each token kind times its price per million tokens. The kinds don't overlap
/// (F1 subtracts cache reads and writes from the input), so the cost is a plain sum.
/// </summary>
public static class CostCalculator
{
    private const decimal TokensPerPrice = 1_000_000m;

    /// <summary>
    /// The record's cost, or null when it can't be known: no price for the model (F2-S3: unknown is never free), or the
    /// provider didn't report input or output tokens. Missing cache counts count as 0 (no cache was used).
    /// </summary>
    public static UsageCost? For(UsageRecord record, PriceTable prices)
    {
        if (prices.Find(record.Model) is not { } price) return null;
        if (record.Tokens is not { Input: { } input, Output: { } output }) return null;

        long cacheRead = record.Tokens.CacheRead ?? 0;
        long cacheWrite = record.Tokens.CacheWrite ?? 0;

        // A long-prompt tier prices the whole request, cache included, once the prompt is over its threshold.
        long prompt = input + cacheRead + cacheWrite;
        TokenPrices tier = price.LongPrompt is { } longPrompt && prompt > longPrompt.AboveTokens ? longPrompt : price;

        // Cache writes at the 5-minute price: the only lifetime used until F4 records which one a request asked for.
        // Reasoning isn't added: it is already part of the output count.
        decimal amount = (input * tier.Input + output * tier.Output + cacheRead * tier.CacheRead +
                          cacheWrite * tier.CacheWrite5m) / TokensPerPrice;

        return new UsageCost(prices.Currency, Math.Round(amount, 6, MidpointRounding.AwayFromZero), prices.AsOf);
    }
}