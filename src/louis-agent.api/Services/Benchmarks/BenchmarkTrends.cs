namespace LouisAgent.Api.Services.Benchmarks;

/// <summary>
/// A check's trend over its runs (F31 step 11a), per mode: what one repeat cost and the share of conversations routed
/// right, a point per finished run, oldest first. Computed in C# over the store (tens of runs × a few conversations),
/// so it's the same on the in-memory store as on MongoDB. A run still going isn't a point: its cost isn't complete.
/// A cost is null, never 0, when none of the mode's requests had a price.
/// </summary>
public static class BenchmarkTrends
{
    /// <summary>One run in one mode.</summary>
    public sealed record Point(string Run, DateTimeOffset At, decimal? Cost, double? Score, int Conversations);

    public sealed record Series(string Name, IReadOnlyList<Point> Points);

    public sealed record Trend(string Check, IReadOnlyList<Series> Modes);

    public static async Task<Trend> ComputeAsync(IBenchmarkStore store, string check, int limit = 20, CancellationToken cancellationToken = default)
    {
        // One run at a time (the runner's rule), so one more than the limit covers a run still going.
        var runs = (await store.ListRunsAsync(check, limit + 1, cancellationToken))
            .Where(r => r.Status != BenchmarkRunStatus.Running).Take(limit).Reverse().ToList();

        var modes = new List<string>();
        var points = new Dictionary<string, List<Point>>();
        foreach (var run in runs)
        {
            var conversations = await store.ConversationsAsync(run.Id, cancellationToken);
            var requests = await store.RequestsAsync(run.Id, cancellationToken);
            foreach (var mode in conversations.GroupBy(c => c.Key.Mode))
            {
                if (!modes.Contains(mode.Key)) { modes.Add(mode.Key); points[mode.Key] = []; }
                var costs = requests.Where(r => r.Turn?.Mode == mode.Key && r.Cost is not null).Select(r => r.Cost!.Value).ToList();
                int right = mode.Count(c => c.Scores.ValueKind == System.Text.Json.JsonValueKind.Object
                                            && c.Scores.TryGetProperty("routed_right", out var v) && v.ValueKind == System.Text.Json.JsonValueKind.True);
                points[mode.Key].Add(new Point(run.Id, run.StartedAt,
                    costs.Count == 0 ? null : costs.Sum() / Math.Max(1, run.Repeat),
                    mode.Any() ? (double)right / mode.Count() : null,
                    mode.Count()));
            }
        }
        return new Trend(check, modes.Select(m => new Series(m, points[m])).ToList());
    }
}
