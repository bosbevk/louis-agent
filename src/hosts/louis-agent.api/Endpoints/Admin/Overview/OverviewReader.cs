using LouisAgent.Api.Contracts.Admin.Overview;
using LouisAgent.Api.Sessions.Log;
using LouisAgent.Core.Pricing.Rates;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Core.Usage.Reports;

namespace LouisAgent.Api.Endpoints.Admin.Overview;

/// <summary>
/// The admin's overview, from the turn log and the ledger joined by session (F127.8): a talk is a session, its part
/// and who answered are its last turn's, and its cost is its requests' in the display currency, unknown when none was
/// priced (ADR 0007).
/// </summary>
/// <remarks>
/// Pure, so a test hands it records and reads the reply: the handler reads the month's two files and the host's
/// currency, and this does the counting.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0007-measure-every-request.md">ADR 0007: measure every request</seealso>
internal static class OverviewReader
{
    private const int LatestTalks = 20;

    /// <summary>The day asked for and its month: the periods, the month's splits and its latest talks.</summary>
    public static OverviewReply Of(DateOnly day, IReadOnlyList<TurnLogRecord> turns,
        IReadOnlyList<UsageRecord> ledger, DisplayCurrency display)
    {
        var month = turns.Where(turn => SameMonth(turn.At, day)).ToList();
        var today = month.Where(turn => DayOf(turn.At) == day).ToList();
        var monthLedger = ledger.Where(record => SameMonth(record.At, day)).ToList();
        var todayLedger = monthLedger.Where(record => DayOf(record.At) == day).ToList();

        // A talk is a session's turns in order; its last turn says where it ended and who answered.
        var talks = month.GroupBy(turn => turn.Session).Select(talk => talk.OrderBy(turn => turn.At).ToList())
            .ToList();
        var latest = talks.OrderByDescending(talk => talk[^1].At).Take(LatestTalks)
            .Select(talk => TalkOf(talk, monthLedger, display)).ToList();

        return new OverviewReply(display.Code, PeriodOf(today, todayLedger, display),
            PeriodOf(month, monthLedger, display), BarsOf(talks, talk => talk[^1].Focus),
            BarsOf(talks, talk => talk[^1].Identity), latest);
    }

    /// <summary>A period's talks, their cost, and the talks that called each tool.</summary>
    private static OverviewPeriod PeriodOf(List<TurnLogRecord> turns, List<UsageRecord> ledger,
        DisplayCurrency display)
    {
        var sessions = turns.Select(turn => turn.Session).ToHashSet();
        var tools = turns.SelectMany(turn => turn.Tools.Select(tool => (Tool: tool, turn.Session))).Distinct()
            .GroupBy(used => used.Tool).Select(used => new OverviewBar(used.Key, used.Count()));
        var spent = ledger.Where(record => record.Session is { } session && sessions.Contains(session));
        return new OverviewPeriod(sessions.Count, CostOf(spent, display), Sorted(tools));
    }

    /// <summary>The talks counted by a name each has, those with none left out, the most first.</summary>
    private static List<OverviewBar> BarsOf(List<List<TurnLogRecord>> talks, Func<List<TurnLogRecord>, string?> nameOf)
    {
        var bars = talks.Select(nameOf).OfType<string>().GroupBy(name => name)
            .Select(named => new OverviewBar(named.Key, named.Count()));
        return Sorted(bars);
    }

    /// <summary>One talk as the list shows it: where it ended, its turns, its cost and the tools it called.</summary>
    private static OverviewTalk TalkOf(List<TurnLogRecord> talk, List<UsageRecord> ledger, DisplayCurrency display)
    {
        var last = talk[^1];
        var spent = ledger.Where(record => record.Session == last.Session);
        var tools = talk.SelectMany(turn => turn.Tools).Distinct().ToList();
        return new OverviewTalk(last.At, last.Page, last.Focus, last.Identity, talk.Count, CostOf(spent, display),
            tools);
    }

    /// <summary>What the requests cost in the display currency; null when none of them was priced.</summary>
    private static decimal? CostOf(IEnumerable<UsageRecord> requests, DisplayCurrency display) =>
        UsageReport.Total("overview", requests).Cost * display.Rate;

    /// <summary>The most talks first, then by name, so the order never shifts between two reads.</summary>
    private static List<OverviewBar> Sorted(IEnumerable<OverviewBar> bars) =>
        bars.OrderByDescending(bar => bar.Talks).ThenBy(bar => bar.Name, StringComparer.Ordinal).ToList();

    /// <summary>The UTC day a time falls on.</summary>
    private static DateOnly DayOf(DateTimeOffset at) => DateOnly.FromDateTime(at.UtcDateTime);

    /// <summary>Whether a time falls in the day's month.</summary>
    private static bool SameMonth(DateTimeOffset at, DateOnly day) =>
        DayOf(at) is var other && other.Year == day.Year && other.Month == day.Month;
}
