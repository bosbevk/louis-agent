namespace LouisAgent.Core.Usage;

using System.Text.Json.Serialization;
using LouisAgent.Core.Llm;

/// <summary>
/// Token counts for one request. The four kinds don't overlap, so they can be added up and priced separately. A count
/// the provider didn't report is null, never 0 (F1-S1).
/// </summary>
/// <remarks>Built from a provider's usage by an <see cref="IUsageMapper"/>.</remarks>
/// <param name="CacheWrite1h">
/// Of <paramref name="CacheWrite"/>, the tokens written with a 1-hour lifetime (F4); the rest were written for 5 minutes.
/// Not a fifth kind: a split of the writes, priced at its own rate. Null when the provider doesn't report the split, and
/// on records from before F4: all writes are then priced at the 5-minute rate, as they were.
/// </param>
public sealed record UsageTokens(
    long? Input, long? CacheWrite, long? CacheRead, long? Output, long? Reasoning,
    [property: JsonPropertyName("cache_write_1h")] long? CacheWrite1h = null);
