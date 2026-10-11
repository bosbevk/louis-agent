using LouisAgent.Core.Config;
using LouisAgent.Core.Usage;
using LouisAgent.Core.Pricing;
using LouisAgent.Core.Llm;

namespace LouisAgent.Core.Tests;

// F4-S4: which lifetime the prefix should have, replayed on the ledger's own request times. Every request here is a
// 1,000,000-token prompt, so costs read straight off Haiku 5.5's per-million prices: write 0.125 (5 min) or 0.20
// (1 hour), read 0.01.
public class CacheGapsTests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 11, 9, 0, 0, TimeSpan.Zero);

    private static readonly PriceTable Prices = new()
    {
        AsOf = "2026-10-10", Currency = "USD",
        Models = new Dictionary<string, ModelPrice>
        {
            ["claude-haiku-5-5"] = new() { Input = 0.10m, Output = 0.50m, CacheRead = 0.01m, CacheWrite5m = 0.125m, CacheWrite1h = 0.20m },
        },
    };

    private static UsageRecord Request(TimeSpan start, string host = "api", string purpose = UsagePurpose.Turn,
        string model = "claude-haiku-5-5", long prompt = 1_000_000, long durationMs = 0) => new(
        At: T0 + start + TimeSpan.FromMilliseconds(durationMs), Round: 1, Purpose: purpose, Host: host, Session: "s", Turn: 1,
        Task: null, Run: null, Service: null, Model: model, Tokens: new UsageTokens(prompt, 0, 0, 10, null),
        DurationMs: durationMs, Stop: "stop");

    private static CacheGapGroup One(params UsageRecord[] records) => CacheGaps.Analyse(records, Prices).Single();

    private static TimeSpan Min(double minutes) => TimeSpan.FromMinutes(minutes);

    [Test]
    public void PausesOf5To60Minutes_RecommendOneHour()
    {
        // Diagram 4: 4 requests 20 minutes apart. 5 min rewrites every time (4 × 0.125); 1 hour writes once, reads 3.
        CacheGapGroup g = One(Request(Min(0)), Request(Min(20)), Request(Min(40)), Request(Min(60)));

        Assert.That((g.Under5m, g.From5To60m, g.Over60m), Is.EqualTo((0, 3, 0)));
        Assert.That(g.FiveMinutes, Is.EqualTo(new LifetimeCost(4, 0, 0.50m)));
        Assert.That(g.OneHour, Is.EqualTo(new LifetimeCost(1, 3, 0.23m)));
        Assert.That(g.Recommended, Is.EqualTo(CacheLifetime.OneHour));
    }

    [Test]
    public void AFixsRounds_SecondsApart_RecommendFiveMinutes()
    {
        // Both lifetimes read the same; 5 minutes wrote at 1.25× instead of 2×.
        CacheGapGroup g = One(Request(TimeSpan.Zero), Request(TimeSpan.FromSeconds(10)), Request(TimeSpan.FromSeconds(20)), Request(Min(2)));

        Assert.That((g.Under5m, g.From5To60m, g.Over60m), Is.EqualTo((3, 0, 0)));
        Assert.That(g.FiveMinutes, Is.EqualTo(new LifetimeCost(1, 3, 0.155m)));
        Assert.That(g.OneHour, Is.EqualTo(new LifetimeCost(1, 3, 0.23m)));
        Assert.That(g.Recommended, Is.EqualTo(CacheLifetime.FiveMinutes));
    }

    [Test]
    public void GapsOverAnHour_RecommendFiveMinutes()
    {
        // Both expire and rewrite every time; 1.25× is less than 2×.
        CacheGapGroup g = One(Request(Min(0)), Request(Min(120)), Request(Min(240)));

        Assert.That(g.Over60m, Is.EqualTo(2));
        Assert.That(g.FiveMinutes, Is.EqualTo(new LifetimeCost(3, 0, 0.375m)));
        Assert.That(g.OneHour, Is.EqualTo(new LifetimeCost(3, 0, 0.60m)));
        Assert.That(g.Recommended, Is.EqualTo(CacheLifetime.FiveMinutes));
    }

    [Test]
    public void AReadRestartsTheClock()
    {
        // 12 minutes after the write, but 4 after the last read: still alive.
        CacheGapGroup g = One(Request(Min(0)), Request(Min(4)), Request(Min(8)), Request(Min(12)));

        Assert.That(g.FiveMinutes, Is.EqualTo(new LifetimeCost(1, 3, 0.155m)));
    }

    [Test]
    public void GapsAreBetweenStarts_NotFinishes()
    {
        // A 2-minute request finishing at 2:00, the next starting at 5:30: 5.5 minutes after the cache was used, so
        // the 5-minute entry has expired, though the finishes are only 3.5 minutes apart.
        CacheGapGroup g = One(Request(Min(0), durationMs: 120_000), Request(Min(5.5), durationMs: 1_000));

        Assert.That(g.FiveMinutes.Writes, Is.EqualTo(2));
        Assert.That(g.From5To60m, Is.EqualTo(1));
    }

    [Test]
    public void EachHostIsItsOwnPrefix_AndSessionsOnAHostShareIt()
    {
        IReadOnlyList<CacheGapGroup> groups = CacheGaps.Analyse(
        [
            Request(Min(0)), Request(Min(20)),
            Request(Min(1), host: "orchestrator", purpose: UsagePurpose.Triage), Request(Min(2), host: "orchestrator", purpose: UsagePurpose.Triage),
        ], Prices);

        Assert.That(groups.Select(g => (g.Host, g.Requests, g.Under5m, g.From5To60m)),
            Is.EqualTo(new[] { ("api", 2, 0, 1), ("orchestrator", 2, 1, 0) }));
    }

    [Test]
    public void SummaryAndCompactionRequests_AreLeftOut()
    {
        // They carry no marker (PromptCachingChatClient skips summaries), so they neither read nor write the prefix.
        CacheGapGroup g = One(Request(Min(0)), Request(Min(3), purpose: UsagePurpose.Summary),
            Request(Min(4), purpose: UsagePurpose.Compaction), Request(Min(20)));

        Assert.That((g.Requests, g.From5To60m), Is.EqualTo((2, 1)));
    }

    [Test]
    public void ThePrefix_IsTheSmallestPrompt()
    {
        CacheGapGroup g = One(Request(Min(0), prompt: 48_000), Request(Min(1), prompt: 61_500), Request(Min(2), prompt: 52_000));

        Assert.That(g.PrefixTokens, Is.EqualTo(48_000));
    }

    [Test]
    public void AModelWithoutAPrice_HasNoCostAndNoRecommendation()
    {
        CacheGapGroup g = One(Request(Min(0), model: "gpt-5"), Request(Min(20), model: "gpt-5"));

        Assert.That((g.FiveMinutes.Cost, g.OneHour.Cost, g.Recommended), Is.EqualTo(((decimal?)null, (decimal?)null, (CacheLifetime?)null)));
        Assert.That(g.OneHour.Reads, Is.EqualTo(1), "the counts are still there");
    }
}