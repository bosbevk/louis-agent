using LouisAgent.Api.Contracts.Admin.Trends;

namespace LouisAgent.Puls.Admin.Trends;

/// <summary>
/// Finds the runs the trend marks: one where someone wrote a note, or where an agent's prompt changed on some line.
/// A run is marked once whatever the number of lines it is on, so a note on the run isn't repeated per mode.
/// </summary>
/// <remarks>
/// Annotations on a time series, drawn in every panel (the unattended run's finding 16, as Grafana's annotations):
/// a plain class beside the component, so what is marked is tested without drawing anything.
/// </remarks>
public static class TrendMarks
{
    /// <summary>The marks, in the axis's order; a run with nothing to say gets none.</summary>
    public static IReadOnlyList<TrendMark> Of(IReadOnlyList<TrendLineRow> series, IReadOnlyList<TrendRun> runs) =>
    [
        .. runs.Select(run => Mark(series, run.Run)).Where(m => m.Notes.Count > 0 || m.Changes.Count > 0)
    ];

    /// <summary>One run's notes, each once, and each line where a prompt changed, with its agents.</summary>
    private static TrendMark Mark(IReadOnlyList<TrendLineRow> series, string run)
    {
        var points = series.Select(s => (s.Name, Point: s.Points.FirstOrDefault(p => p.Run == run)))
            .Where(s => s.Point is not null)
            .ToList();

        // A note on the run comes with every line's point, so the same text is listed once.
        var notes = points.SelectMany(s => s.Point!.Notes ?? []).Distinct().ToArray();
        // Null (couldn't tell) and empty (nothing changed) both mean no marker.
        var changes = points.Where(s => s.Point!.PromptChanged is { Count: > 0 })
            .Select(s => $"{s.Name}: {string.Join(", ", s.Point!.PromptChanged!)}")
            .ToArray();
        return new TrendMark(run, notes, changes);
    }
}
