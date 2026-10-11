namespace louis_agent.core.usage;

using louis_agent.core.config;

/// <summary>
/// Which cache lifetime the prefix (tools + system prompt, F4's marker 1) should have, measured from the ledger (F4-S4):
/// the gaps between requests that share it, and what each lifetime would have cost on exactly those requests.
/// </summary>
/// <remarks>
/// Each host and model is its own prefix (the orchestrator's tools and runbooks are a different entry from louis-agent's),
/// and every session on a host shares it, so requests are taken in time order across sessions. Replayed per lifetime:
/// a request within the lifetime of the last one reads the prefix (and restarts the clock); any other request writes it.
/// Summary and compaction requests carry no marker and are left out.
/// </remarks>
public static class CacheGaps
{
    private static readonly TimeSpan FiveMinutes = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan OneHour = TimeSpan.FromHours(1);
    private const decimal TokensPerPrice = 1_000_000m;

    public static IReadOnlyList<CacheGapGroup> Analyse(IEnumerable<UsageRecord> records, PriceTable? prices) =>
        records
            .Where(r => r.Purpose is not (UsagePurpose.Summary or UsagePurpose.Compaction) && r.Tokens.Input is not null)
            .GroupBy(r => (Host: r.Host ?? "(no host)", Model: r.Model ?? "(no model)"))
            .Select(g => Group(g.Key.Host, g.Key.Model, g.ToList(), prices?.Find(g.Key.Model)))
            .OrderBy(g => g.Host, StringComparer.Ordinal).ThenBy(g => g.Model, StringComparer.Ordinal)
            .ToList();

    private static CacheGapGroup Group(string host, string model, List<UsageRecord> records, ModelPrice? price)
    {
        // When the request started: the cache is read (and its clock restarted) when the request arrives.
        List<DateTimeOffset> starts = records.Select(r => r.At - TimeSpan.FromMilliseconds(r.DurationMs)).Order().ToList();
        List<TimeSpan> gaps = starts.Zip(starts.Skip(1), (a, b) => b - a).ToList();

        // The prefix is in every request, so no request's prompt is smaller: the smallest prompt is an upper bound on it,
        // close to it on a session's first request (prefix + one short message).
        long prefix = records.Min(r => r.Tokens.Input!.Value + (r.Tokens.CacheRead ?? 0) + (r.Tokens.CacheWrite ?? 0));

        return new CacheGapGroup(
            host, model, starts.Count, prefix,
            Under5m: gaps.Count(g => g <= FiveMinutes),
            From5To60m: gaps.Count(g => g > FiveMinutes && g <= OneHour),
            Over60m: gaps.Count(g => g > OneHour),
            FiveMinutes: Replay(starts, FiveMinutes, prefix, price?.CacheWrite5m, price?.CacheRead),
            OneHour: Replay(starts, OneHour, prefix, price?.CacheWrite1h, price?.CacheRead));
    }

    internal static LifetimeCost Replay(IReadOnlyList<DateTimeOffset> starts, TimeSpan lifetime, long prefix, decimal? writePrice, decimal? readPrice)
    {
        int writes = 0, reads = 0;
        DateTimeOffset? last = null;
        foreach (DateTimeOffset start in starts)
        {
            if (last is { } previous && start - previous <= lifetime) reads++;
            else writes++;
            last = start;
        }

        decimal? cost = writePrice is { } write && readPrice is { } read
            ? Math.Round(prefix * (writes * write + reads * read) / TokensPerPrice, 6, MidpointRounding.AwayFromZero)
            : null;
        return new LifetimeCost(writes, reads, cost);
    }
}

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

/// <summary>What one lifetime would have cost the prefix: its writes, its reads and their price (null: no price).</summary>
public sealed record LifetimeCost(int Writes, int Reads, decimal? Cost);