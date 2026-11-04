using LouisAgent.Puls.Admin.Charts;
using LouisAgent.Puls.Admin.Explorer.Tree;

namespace LouisAgent.Puls.Admin.Explorer.Timeline;

/// <summary>
/// A turn's requests on a time axis from the first request's start to the last one's end: Puls's lane, then the
/// specialist's, each request a bar from its start to its end.
/// </summary>
public sealed record TimelineLayout(IReadOnlyList<TimelineLane> Lanes, double SpanMs)
{
    /// <summary>Lays out a turn that made at least one request.</summary>
    /// <remarks>A bar is never thinner than 0.6% of the axis, so a quick request can still be clicked.</remarks>
    public static TimelineLayout Of(TurnNode turn)
    {
        // The axis runs from the first request's start to the last one's end.
        var start = turn.Rows.Min(r => r.Start);
        double span = Math.Max(1, (turn.Rows.Max(r => r.At) - start).TotalMilliseconds);

        // Each lane's requests as bars, placed and sized as shares of the axis.
        TimelineLane Lane(string label, bool specialist) => new(label, specialist,
        [
            .. turn.Rows.Where(r => r.Specialist == specialist).Select((r, i) => new TimelineBar(r, i,
                ChartNumbers.Format((r.Start - start).TotalMilliseconds / span * 100),
                ChartNumbers.Format(Math.Max(0.6, r.DurationMs / span * 100))))
        ]);

        return new TimelineLayout([Lane("Puls", false), Lane(turn.Role ?? "specialist", true)], span);
    }
}
