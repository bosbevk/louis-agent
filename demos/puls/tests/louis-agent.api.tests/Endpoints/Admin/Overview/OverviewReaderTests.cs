using LouisAgent.Api.Contracts.Admin.Overview;
using LouisAgent.Api.Endpoints.Admin.Overview;
using LouisAgent.Api.Sessions.Log;
using LouisAgent.Core.Pricing.Rates;
using LouisAgent.Core.Usage.Records;

namespace LouisAgent.Api.Tests.Endpoints.Admin.Overview;

/// <summary>
/// F127.8, Louis's pick of three designs, A. Overview first: the turn log and the ledger joined by session into today
/// and this month (talks, cost in the display currency, the talks that used each tool), the talks by part and by
/// identity, and the latest talks.
/// </summary>
public class OverviewReaderTests
{
    private static readonly DateOnly Day = new(2026, 11, 12);
    private static readonly DateTimeOffset Evening = new(2026, 11, 12, 19, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Earlier = new(2026, 11, 3, 10, 0, 0, TimeSpan.Zero);

    /// <summary>Rand at R17 to the dollar.</summary>
    private static readonly DisplayCurrency Rand = new("ZAR", "R", 17m, "2026-11-01", "USD", "test");

    /// <summary>A turn of the log.</summary>
    private static TurnLogRecord Turn(DateTimeOffset at, string session, int turn, string? identity, string page,
        string focus, params string[] tools) => new(at, session, turn, identity, page, focus, tools);

    /// <summary>One request of the ledger, its cost in dollars, or none.</summary>
    private static UsageRecord Request(DateTimeOffset at, string session, decimal? usd) =>
        new(at, 1, "turn", "api", session, 1, null, null, null, "model", new UsageTokens(1, 0, 0, 1, null), 10, "end",
            usd is { } amount ? new UsageCost("USD", amount, "2026-11-01") : null);

    /// <summary>An evening of three talks, and one earlier in the month.</summary>
    private static OverviewReply Evening3()
    {
        TurnLogRecord[] turns =
        [
            Turn(Earlier, "s0", 1, "louis-agent-guide", "/", "selling"),
            Turn(Evening, "s1", 1, "louis-agent-guide", "/pricing", "selling", "Offer"),
            Turn(Evening.AddMinutes(2), "s1", 2, "louis-agent-guide", "/pricing", "selling", "AskFor", "SendLead"),
            Turn(Evening.AddMinutes(5), "s2", 1, "koringland-attorneys", "/law-practice-demo", "demos", "AskFor"),
            Turn(Evening.AddMinutes(9), "s3", 1, "louis-agent-guide", "/framework/packs", "framework"),
        ];
        UsageRecord[] ledger =
        [
            Request(Earlier, "s0", 0.01m),
            Request(Evening, "s1", 0.005m),
            Request(Evening.AddMinutes(2), "s1", 0.003m),
            Request(Evening.AddMinutes(5), "s2", 0.004m),
            Request(Evening.AddMinutes(9), "s3", null),
        ];
        return OverviewReader.Of(Day, turns, ledger, Rand);
    }

    /// <summary>Today and the month: talks, the cost in rand, and the talks that used each tool.</summary>
    [Test]
    public void TodayAndTheMonth_CountTalksCostAndTools()
    {
        var reply = Evening3();

        Assert.Multiple(() =>
        {
            Assert.That(reply.Currency, Is.EqualTo("ZAR"));
            Assert.That((reply.Today.Talks, reply.Today.Cost), Is.EqualTo((3, 0.204m)));
            Assert.That((reply.Month.Talks, reply.Month.Cost), Is.EqualTo((4, 0.374m)));
            Assert.That(reply.Today.Tools.Select(bar => (bar.Name, bar.Talks)),
                Is.EqualTo(new[] { ("AskFor", 2), ("Offer", 1), ("SendLead", 1) }));
        });
    }

    /// <summary>The month's talks by part of the site and by who answered, the most first.</summary>
    [Test]
    public void TheMonth_SplitsByPartAndIdentity()
    {
        var reply = Evening3();

        Assert.Multiple(() =>
        {
            Assert.That(reply.ByPart.Select(bar => (bar.Name, bar.Talks)),
                Is.EqualTo(new[] { ("selling", 2), ("demos", 1), ("framework", 1) }));
            Assert.That(reply.ByIdentity.Select(bar => (bar.Name, bar.Talks)),
                Is.EqualTo(new[] { ("louis-agent-guide", 3), ("koringland-attorneys", 1) }));
        });
    }

    /// <summary>The latest talks, newest first, each with its last page, its turns, its cost and its tools.</summary>
    [Test]
    public void TheLatestTalks_AreNewestFirst()
    {
        var latest = Evening3().Latest;

        Assert.Multiple(() =>
        {
            Assert.That(latest.Select(talk => talk.Page),
                Is.EqualTo(new[] { "/framework/packs", "/law-practice-demo", "/pricing", "/" }));
            Assert.That((latest[2].Turns, latest[2].Cost), Is.EqualTo((2, 0.136m)));
            Assert.That(latest[2].Tools, Is.EqualTo(new[] { "Offer", "AskFor", "SendLead" }));
            Assert.That(latest[0].Cost, Is.Null, "an unpriced talk's cost is unknown, never 0 (ADR 0007)");
        });
    }
}
