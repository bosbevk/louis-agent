namespace louis_agent.core.usage;

using Microsoft.Extensions.AI;

/// <summary>
/// Turns a provider adapter's <see cref="UsageDetails"/> into the ledger's four non-overlapping token kinds. One per
/// provider, chosen by <c>LlmClientFactory.CreateUsageMapper</c>, so provider differences stay out of the recorder.
/// </summary>
public interface IUsageMapper
{
    /// <summary>Maps one request's usage. Null usage, or a count the provider didn't report, maps to null, never 0.</summary>
    UsageTokens Map(UsageDetails? usage);
}

/// <summary>
/// The Microsoft.Extensions.AI contract, used for any provider without its own mapper: cached tokens are part of
/// <see cref="UsageDetails.InputTokenCount"/>, so the uncached input is the input minus cache reads and writes.
/// <see cref="UsageDetails"/> has no cache-write property, so a provider that reports writes overrides
/// <see cref="CacheWriteTokens"/>.
/// </summary>
public class StandardUsageMapper : IUsageMapper
{
    public UsageTokens Map(UsageDetails? usage)
    {
        if (usage is null) return new UsageTokens(null, null, null, null, null);

        long? cacheRead = usage.CachedInputTokenCount;
        long? cacheWrite = CacheWriteTokens(usage);
        // Not clamped at 0: a negative input means an adapter broke the contract, and the ledger should show it.
        long? input = usage.InputTokenCount - (cacheRead ?? 0) - (cacheWrite ?? 0);

        return new UsageTokens(input, cacheWrite, cacheRead, usage.OutputTokenCount, usage.ReasoningTokenCount);
    }

    /// <summary>Tokens written to the cache by this request, or null when the provider doesn't report them.</summary>
    protected virtual long? CacheWriteTokens(UsageDetails usage) => null;
}

/// <summary>
/// The Anthropic adapter reports cache writes in <see cref="UsageDetails.AdditionalCounts"/> (measured in F1 step 1;
/// see the optimisation spec, "What the Anthropic adapter reports").
/// </summary>
public sealed class AnthropicUsageMapper : StandardUsageMapper
{
    internal const string CacheCreationKey = "CacheCreationInputTokens";

    protected override long? CacheWriteTokens(UsageDetails usage) =>
        usage.AdditionalCounts?.TryGetValue(CacheCreationKey, out long written) == true ? written : null;
}
