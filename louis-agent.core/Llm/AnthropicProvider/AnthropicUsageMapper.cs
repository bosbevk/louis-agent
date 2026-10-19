namespace LouisAgent.Core.Llm.AnthropicProvider;

using LouisAgent.Core.Llm.UsageMapping;
using LouisAgent.Core.Usage.Records;
using Anthropic.Models.Messages;
using Microsoft.Extensions.AI;

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
