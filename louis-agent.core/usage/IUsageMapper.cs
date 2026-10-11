namespace louis_agent.core.usage;

using Anthropic.Models.Messages;
using Microsoft.Extensions.AI;

/// <summary>
/// Turns a provider adapter's <see cref="UsageDetails"/> into the ledger's four non-overlapping token kinds. One per
/// provider, chosen by <c>LlmClientFactory.CreateUsageMapper</c>, so provider differences stay out of the recorder.
/// </summary>
public interface IUsageMapper
{
    /// <summary>Maps one request's usage. Null usage, or a count the provider didn't report, maps to null, never 0.</summary>
    UsageTokens Map(UsageDetails? usage);

    /// <summary>
    /// Whether the recorder should keep this raw object (the provider's own response, or one of its stream events) for
    /// <see cref="Map(UsageDetails?, IReadOnlyList{object})"/>: for counts <see cref="UsageDetails"/> has no field for.
    /// </summary>
    bool Keeps(object raw);

    /// <summary>Maps one request's usage with the raw objects <see cref="Keeps"/> chose, in the order they came.</summary>
    UsageTokens Map(UsageDetails? usage, IReadOnlyList<object> raw);
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

    /// <summary>Keeps nothing: the contract's counts are all there is.</summary>
    public virtual bool Keeps(object raw) => false;

    public virtual UsageTokens Map(UsageDetails? usage, IReadOnlyList<object> raw) => Map(usage);

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

    /// <summary>
    /// The writes split by lifetime (F4) are only on the SDK's own usage: the <see cref="Message"/> of a response, and on
    /// a stream the <c>message_start</c> event's message (the closing <c>message_delta</c> has the total only; measured
    /// in F4 step 6).
    /// </summary>
    public override bool Keeps(object raw) => Usage(raw) is not null;

    public override UsageTokens Map(UsageDetails? usage, IReadOnlyList<object> raw)
    {
        UsageTokens tokens = Map(usage);
        // Not reported is null, never 0 (F1): with no writes reported there is nothing to split.
        if (tokens.CacheWrite is null) return tokens;
        return tokens with { CacheWrite1h = raw.Select(r => Usage(r)?.CacheCreation).OfType<CacheCreation>().FirstOrDefault()?.Ephemeral1hInputTokens };
    }

    private static Usage? Usage(object raw) => raw switch
    {
        Message message => message.Usage,
        RawMessageStreamEvent e when e.TryPickStart(out RawMessageStartEvent? start) => start.Message.Usage,
        _ => null,
    };
}
