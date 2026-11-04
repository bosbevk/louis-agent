namespace LouisAgent.Core.Llm.UsageMapping;

using LouisAgent.Core.Usage.Records;
using Anthropic.Models.Messages;
using Microsoft.Extensions.AI;

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

    /// <summary>Keeps nothing: the contract's counts are all there is.</summary>
    public virtual bool Keeps(object raw) => false;

    public virtual UsageTokens Map(UsageDetails? usage, IReadOnlyList<object> raw) => Map(usage);

    /// <summary>Tokens written to the cache by this request, or null when the provider doesn't report them.</summary>
    protected virtual long? CacheWriteTokens(UsageDetails usage) => null;
}
