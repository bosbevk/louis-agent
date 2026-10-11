namespace LouisAgent.Core.Usage;

using LouisAgent.Core.Config;

/// <summary>One prefix (a host and model): its requests, the gaps between them, and each lifetime replayed on them.</summary>
/// <param name="PrefixTokens">Estimated: the group's smallest prompt (input + cache read + cache write).</param>
public sealed record CacheGapGroup(
    string Host, string Model, int Requests, long PrefixTokens, int Under5m, int From5To60m, int Over60m,
    LifetimeCost FiveMinutes, LifetimeCost OneHour)
{
    /// <summary>The cheaper lifetime; 5 minutes on a tie (the default). Null when the model has no price.</summary>
    public CacheLifetime? Recommended =>
        FiveMinutes.Cost is { } five && OneHour.Cost is { } hour ? (hour < five ? CacheLifetime.OneHour : CacheLifetime.FiveMinutes) : null;
}
