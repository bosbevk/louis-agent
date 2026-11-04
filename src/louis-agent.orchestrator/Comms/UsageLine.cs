using System.Text.Json.Serialization;
using LouisAgent.Core.Usage.Reports;

namespace LouisAgent.Orchestrator.Comms;

/// <summary>
/// What a triage or a fix used, as the orchestrator's jsonl files write it (decisions.jsonl, fixes.jsonl). The field
/// names are louis-agent.api's, so every file reads alike.
/// </summary>
/// <remarks>
/// A named record instead of an anonymous object (ADR 0041): the shape is written down once, and the files that carry
/// it can be named records too. The currency is left out when nothing was priced, so an unpriced line never claims one.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
internal sealed record UsageLine(
    [property: JsonPropertyName("requests")] int Requests,
    [property: JsonPropertyName("input_tokens")] long InputTokens,
    [property: JsonPropertyName("cache_creation_input_tokens")] long CacheCreationInputTokens,
    [property: JsonPropertyName("cache_read_input_tokens")] long CacheReadInputTokens,
    [property: JsonPropertyName("output_tokens")] long OutputTokens,
    [property: JsonPropertyName("cost")] decimal? Cost,
    [property: JsonPropertyName("currency")] string? Currency,
    [property: JsonPropertyName("unpriced")] int Unpriced)
{
    /// <summary>The line for a usage total, in the charged currency; nothing when it wasn't measured.</summary>
    public static UsageLine? Of(UsageTotal? usage, string? currency) =>
        usage is null
            ? null
            : new UsageLine(usage.Requests, usage.Input, usage.CacheWrite, usage.CacheRead, usage.Output, usage.Cost,
                usage.Cost is null ? null : currency, usage.Unpriced);
}
