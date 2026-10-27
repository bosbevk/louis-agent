using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;

namespace LouisAgent.Api.Benchmarks.Reports;

/// <summary>
/// A check's trend per category, per model (F18 step 5c.4): for each category (and the overall line) a line per mode on its model, a point per run that has a rate for it. The points
/// are the runner's numbers (the slices of each run's roll-up); nothing is computed here. A slice with too few trials has no rate, so it is no point: the line has a gap there, never a
/// 0 (ADR 0007). The runs are the overall trend's (<see cref="BenchmarkTrends.FinishedRunsAsync"/>: a replay, a re-score, a held-out run and a run still going are no point), and a
/// run from before 5c has no slices and adds none. A category no run ever had enough trials for has no chart.
/// </summary>
public static class CategoryTrends
{
    /// <summary>One run in one line: the runner's rates for the slice, and its n.</summary>
    public sealed record Point(
        string Run,
        DateTimeOffset At,
        double? PassRate,
        double? PassAtK,
        double? PassHatK,
        int N);

    /// <summary>A mode on one model, named as the overall trend names it: the mode, with its model in brackets when the model is named.</summary>
    public sealed record Line(string Name, IReadOnlyList<Point> Points, string? Model = null);

    public sealed record Category(string Name, IReadOnlyList<Line> Lines);

    public static async Task<IReadOnlyList<Category>> ComputeAsync(IBenchmarkStore store, string check, int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var runs = await BenchmarkTrends.FinishedRunsAsync(store, check, limit, cancellationToken);

        var lines = new Dictionary<(string Category, string Mode, string? Model), List<Point>>();
        foreach (var run in runs)
            foreach (var slice in run.Slices ?? [])
            {
                if (slice.TooFew || slice.PassRate is null) continue;
                string? model = run.Models
                    ?.FirstOrDefault(m => m.Mode == slice.Mode && m.Level == BenchmarkModelLevel.Agent)?.Model;
                if (!lines.TryGetValue((slice.Slice, slice.Mode, model), out var points))
                    lines[(slice.Slice, slice.Mode, model)] = points = [];
                points.Add(new Point(run.Id, run.StartedAt, slice.PassRate, slice.PassAtK, slice.PassHatK, slice.N));
            }

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
