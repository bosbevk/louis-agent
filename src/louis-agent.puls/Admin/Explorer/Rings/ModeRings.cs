using LouisAgent.Puls.Admin.Charts;
using LouisAgent.Puls.Admin.Explorer.Legends;
using LouisAgent.Puls.Admin.Explorer.Text;
using LouisAgent.Puls.Admin.Explorer.Tree;
using LouisAgent.Web.Shared.Usage;
using static LouisAgent.Puls.Admin.Explorer.Legends.ExplorerColours;

namespace LouisAgent.Puls.Admin.Explorer.Rings;

/// <summary>
/// The donut each "per mode" chart draws for one mode: what it cost and how long it took (Puls's part against the
/// specialist's), its tokens, the conversations that passed, the cache, its tools, the sites it read, and what a
/// passed conversation cost.
/// </summary>
/// <remarks>
/// A figure per chart, so the charts are one component that draws whichever it is handed, and the sums live where a
/// test can reach them. An unknown cost stays unknown (ADR 0007): the ring draws what is known, the titles say "—".
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0007-measure-every-request.md">ADR 0007: measure every request</seealso>
public static class ModeRings
{
    /// <summary>What Puls's own requests cost against the specialist's, the mode's cost in the middle.</summary>
    public static RingFigure Cost(ModeNode m, CurrencyChoice? currency)
    {
        var rows = m.Rows;
        decimal? own = RequestSums.Cost(rows, specialist: false), spec = RequestSums.Cost(rows, specialist: true);
        return new RingFigure([(double)(own ?? 0), (double)(spec ?? 0)], [Own, Spec],
            [
                $"Puls {ExplorerText.Money(own, currency)}",
                $"{ModeNames.Roles([m])} {ExplorerText.Money(spec, currency)}"
            ],
            ExplorerText.Money(m.Cost, currency), $"{rows.Count} requests");
    }

    /// <summary>The four kinds of token, their total in the middle.</summary>
    public static RingFigure Tokens(ModeNode m)
    {
        long[] parts = RequestSums.Tokens(m.Rows);
        return new RingFigure([.. parts.Select(p => (double)p)], TokenKinds.Colours, TokenKinds.Titles(parts),
            UsageFormat.Tokens(parts.Sum()), "tokens");
    }

    /// <summary>The model's seconds, Puls's against the specialist's, a turn's average time in the middle.</summary>
    public static RingFigure Time(ModeNode m)
    {
        double own = RequestSums.Seconds(m.Rows, specialist: false),
            spec = RequestSums.Seconds(m.Rows, specialist: true);
        var turns = m.Turns;
        string perTurn = turns.Count == 0 ? "—" : $"{ExplorerText.Secs(turns.Average(t => t.Seconds))} s";
        return new RingFigure([own, spec], [Own, Spec],
            [$"Puls {ExplorerText.Secs(own)} s", $"{ModeNames.Roles([m])} {ExplorerText.Secs(spec)} s"], perTurn,
            "per turn");
    }

    /// <summary>Conversations that passed against those that didn't, the share passed in the middle.</summary>
    public static RingFigure Passed(ModeNode m)
    {
        int total = m.Conversations.Count, passed = m.Conversations.Count(c => c.Passed);
        return new RingFigure([passed, total - passed], [Right, Wrong],
            [$"passed {passed}", $"not passed {total - passed}"],
            total == 0 ? "—" : $"{(double)passed / total:0%}", $"{passed} / {total}");
    }

    /// <summary>Input read from the cache against input sent fresh, the hit share in the middle.</summary>
    public static RingFigure Cache(ModeNode m)
    {
        var rows = m.Rows;
        long cached = rows.Sum(r => r.CacheRead ?? 0), fresh = rows.Sum(r => (r.Input ?? 0) + (r.CacheWrite ?? 0));
        long all = cached + fresh;
        return new RingFigure([cached, fresh], [ChartPalette.Series(2), ChartPalette.Series(0)],
            [$"from the cache {cached:N0}", $"fresh {fresh:N0}"], all == 0 ? "—" : $"{(double)cached / all:0%}",
            "from the cache");
    }

    /// <summary>Tool calls that worked against those refused or failed, the failures in the middle.</summary>
    public static RingFigure Tools(ModeNode m)
    {
        var turns = m.Turns;
        int calls = turns.Sum(t => t.ToolCalls), failed = turns.Sum(t => t.ToolsFailed);
        return new RingFigure([calls - failed, failed], [Right, Wrong],
            [$"worked {calls - failed}", $"refused or failed {failed}"], calls == 0 ? "—" : failed.ToString(),
            $"of {calls}");
    }

    /// <summary>The sites read by kind, how many different ones in the middle.</summary>
    public static RingFigure Sources(SiteCounts sites) =>
        new([sites.Shops, sites.Za, sites.Maps, sites.Rest],
            [Right, ChartPalette.Series(0), ChartPalette.Series(1), Other],
            [$"SA shops {sites.Shops}", $"other SA {sites.Za}", $"maps {sites.Maps}", $"elsewhere {sites.Rest}"],
            sites.Hosts.Count.ToString(), sites.Hosts.Count == 1 ? "site" : "sites");

    /// <summary>
    /// The cost of the conversations that passed (a real answer) against those that didn't, what one passed
    /// conversation cost in the middle: the API's cost per answer.
    /// </summary>
    public static RingFigure Value(ModeNode m, CurrencyChoice? currency)
    {
        var answered = m.Conversations.Where(c => c.Passed).ToList();
        // Unknown stays unknown (ADR 0007): no priced request, no amount; the ring draws what is known.
        decimal? good = UsageFormat.KnownSum(answered.Select(c => c.Cost)), bad = m.Cost - (good ?? 0);
        if (answered.Count == 0)
            return new RingFigure([(double)(good ?? 0), (double)(bad ?? 0)], [Right, Wrong],
                ValueTitles(good, bad, currency), "—", "no answer");

        return new RingFigure([(double)(good ?? 0), (double)(bad ?? 0)], [Right, Wrong],
            ValueTitles(good, bad, currency), ExplorerText.Money(good / answered.Count, currency), "per answer");
    }

    /// <summary>The model's seconds on all a mode's requests, Puls's and the specialist's.</summary>
    public static double ModelSeconds(ModeNode m) =>
        RequestSums.Seconds(m.Rows, specialist: false) + RequestSums.Seconds(m.Rows, specialist: true);

    /// <summary>The value ring's two titles: what the answered conversations cost, and what the rest did.</summary>
    private static IReadOnlyList<string> ValueTitles(decimal? good, decimal? bad, CurrencyChoice? currency) =>
        [$"with an answer {ExplorerText.Money(good, currency)}", $"without {ExplorerText.Money(bad, currency)}"];
}
