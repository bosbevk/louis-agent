namespace LouisAgent.Puls.Admin.Trends;

/// <summary>A run on the trend's x axis.</summary>
public sealed record TrendRun(string Run, DateTimeOffset At);

/// <summary>One mode's value at a run in one panel; null when nobody measured it (never 0).</summary>
public sealed record TrendSample(string Run, DateTimeOffset At, double? Value);

/// <summary>A mode's reading in a panel's head: its value at the shown run, and the change since its run before (null when there is none or it is nil).</summary>
public sealed record TrendValue(string Name, string Color, string Text, string? Change, bool Up);

/// <summary>
/// What the trend chart reads out of its runs (the rework of 2026-10-26): one run is read across the panels, the marked one, else the last; hovering a point reads that run
/// in its own panel only (Louis: "only update the graph I'm on"); a mode's value at that run comes with its change since the run before it.
/// </summary>
public sealed class TrendReading(IReadOnlyList<TrendRun> runs, string? marked)
{
    public TrendRun Shown => runs.FirstOrDefault(r => r.Run == marked) ?? runs[^1];

    /// <summary>The run a panel reads: the hovered one while the mouse is on a point of that panel, else the shown run.</summary>
    public TrendRun ShownIn(string topic, (string Topic, string Run)? hover) =>
        hover is { } h && h.Topic == topic ? runs.FirstOrDefault(r => r.Run == h.Run) ?? Shown : Shown;

    /// <summary>The run the panel reads because of a hover, null when the mouse is elsewhere (the panel then says nothing extra).</summary>
    public TrendRun? Hovered(string topic, (string Topic, string Run)? hover) =>
        hover is { } h && h.Topic == topic ? ShownIn(topic, hover) : null;

    public static TrendValue Value(string name, string color, IEnumerable<TrendSample> points, string run,
        Func<double, string> label)
    {
        var known = points.Where(p => p.Value is not null).OrderBy(p => p.At).ToList();
        int at = known.FindIndex(p => p.Run == run);
        if (at < 0) return new TrendValue(name, color, "–", null, false);
        double value = known[at].Value!.Value, delta = at > 0 ? value - known[at - 1].Value!.Value : 0;
        return new TrendValue(name, color, label(value),
            delta == 0 ? null : (delta > 0 ? "▲ +" : "▼ −") + label(Math.Abs(delta)), delta > 0);
    }
}
