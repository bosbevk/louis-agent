namespace LouisAgent.Core.Usage;

/// <summary>Totals of a group of usage records: tokens by kind, requests, and cost.</summary>
/// <param name="Cost">The sum of the priced records; null when none of them has a cost.</param>
/// <param name="Unpriced">Records without a cost ("price unknown"); their tokens are in the totals, their cost isn't.</param>
public sealed record UsageTotal(
    string Key, int Requests, long Input, long CacheWrite, long CacheRead, long Output, decimal? Cost, int Unpriced);
