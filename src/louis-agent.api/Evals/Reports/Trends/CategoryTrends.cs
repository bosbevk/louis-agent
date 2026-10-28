using LouisAgent.Api.Evals.Runs.Kinds;
using LouisAgent.Api.Evals.Store;

namespace LouisAgent.Api.Evals.Reports.Trends;

/// <summary>
/// Draws an eval's trend per category: for each category, a line per mode on its model and a point per run. The
/// numbers are the runner's own rates; nothing is worked out here. A slice with too few trials has no rate, so the
/// line has a gap there, never a 0, and a category no run had enough trials for gets no chart.
/// </summary>
/// <remarks>
/// It uses the same runs as the overall trend (<see cref="EvalTrends.FinishedRunsAsync"/>), so a replay, a
/// re-score, a held-out run and a run still going are no point. A run from before slices were kept adds nothing.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0007-measure-every-request.md">ADR 0007: measure every request</seealso>
public static class CategoryTrends
{
    /// <summary>One run on one line: the runner's rates for the slice, and how many trials it had.</summary>
    public sealed record Point(
        string Run,
        DateTimeOffset At,
        double? PassRate,
        double? PassAtK,
        double? PassHatK,
        int N);

    /// <summary>
    /// A mode on one model, named as the overall trend names it: the mode, with its model in brackets when known.
    /// </summary>
    public sealed record Line(string Name, IReadOnlyList<Point> Points, string? Model = null);

    /// <summary>One category's chart: its lines.</summary>
    public sealed record Category(string Name, IReadOnlyList<Line> Lines);

    /// <summary>Builds the charts for the eval's last runs, one per category, sorted by name.</summary>
    public static async Task<IReadOnlyList<Category>> ComputeAsync(IEvalStore store, string check, int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var runs = await EvalTrends.FinishedRunsAsync(store, check, limit, cancellationToken);

        // Collect the points by category, mode and model; a slice without a rate is a gap, so it adds no point.
        var lines = new Dictionary<(string Category, string Mode, string? Model), List<Point>>();
        foreach (var run in runs)
            foreach (var slice in run.Slices ?? [])
            {
                if (slice.TooFew || slice.PassRate is null) continue;
                string? model = run.Models
                    ?.FirstOrDefault(m => m.Mode == slice.Mode && m.Level == EvalModelLevel.Agent)?.Model;
                if (!lines.TryGetValue((slice.Slice, slice.Mode, model), out var points))
                    lines[(slice.Slice, slice.Mode, model)] = points = [];
                points.Add(new Point(run.Id, run.StartedAt, slice.PassRate, slice.PassAtK, slice.PassHatK, slice.N));
            }

        // A chart per category, its lines named the way the overall trend names them.
        return
        [
            .. lines.GroupBy(line => line.Key.Category).OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(group => new Category(group.Key,
                [
                    .. group.Select(line =>
                        new Line(line.Key.Model is null ? line.Key.Mode : $"{line.Key.Mode} ({line.Key.Model})",
                            line.Value, line.Key.Model))
                ]))
        ];
    }
}
