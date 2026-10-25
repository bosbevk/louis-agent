using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using System.Text.Json;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;

namespace LouisAgent.Api.Benchmarks.Reports;

/// <summary>
/// A check's trend over its runs (F31 step 11a), per mode, a point per finished run, oldest first: the nine rings of the
/// explorer over time (Louis). What one repeat cost and its tokens, seconds per turn, the share routed right, the share
/// of ingredients priced with a source, the share of input from the cache, the share of tool calls refused, what one
/// real answer cost, and how many sites were read. Computed in C# over the store, so it's the same on the in-memory
/// store as on MongoDB. A run still going isn't a point: its cost isn't complete. A value nobody measured is null,
/// never 0.
/// </summary>
public static class BenchmarkTrends
{
    /// <summary>One run in one mode.</summary>
    public sealed record Point(string Run, DateTimeOffset At, decimal? Cost, double? Score, int Conversations,
        long? Tokens = null, double? SecondsPerTurn = null, double? CacheShare = null, double? Sourced = null,
        double? ToolsFailed = null, decimal? CostPerAnswer = null, int? Sites = null, double? PassAtK = null, double? PassHatK = null, int? K = null);

    /// <summary>One line of the trend: a mode on one model. Its name is the mode, with its model in brackets when the model is named (F18 step 2a).</summary>
    public sealed record Series(string Name, IReadOnlyList<Point> Points, string? Model = null);

    public sealed record Trend(string Check, IReadOnlyList<Series> Modes);

    /// <summary>A meal's real answer: at least this share of its ingredients priced with a source (as the explorer's Waarde ring).</summary>
    private const double AnsweredShare = 0.8;

    private static string SeriesName(string mode, string? model) => model is null ? mode : $"{mode} ({model})";

    public static async Task<Trend> ComputeAsync(IBenchmarkStore store, string check, int limit = 20, CancellationToken cancellationToken = default)
    {
        // One run at a time (the runner's rule), so one more than the limit covers a run still going.
        var runs = (await store.ListRunsAsync(check, limit + 1, cancellationToken))
            .Where(r => r.Status != BenchmarkRunStatus.Running)
            // The trend is the dev scenarios (F18 step 5.4 review): a held-out run, or one of all of them, is a different set of conversations, and a run
            // before splits were kept (no split) was a dev run. Joining them would draw a line through two different things.
            .Where(r => r.Split is null or BenchmarkRunSplit.Dev)
            // A replay (F18 step 5.5) is the recorded run again: its scores are that run's and it costs nothing, so it is no point of its own.
            .Where(r => r.Settings is null || !r.Settings.ContainsKey("replay_of"))
            .Take(limit).Reverse().ToList();

        // A line is a mode on one model: runs on two models of one mode are two lines, never one line joining them.
        var lines = new List<(string Mode, string? Model)>();
        var points = new Dictionary<(string Mode, string? Model), List<Point>>();
        foreach (var run in runs)
        {
            var conversations = await store.ConversationsAsync(run.Id, cancellationToken);
            var requests = await store.RequestsAsync(run.Id, cancellationToken);
            int repeats = Math.Max(1, run.Repeat);
            foreach (var mode in conversations.GroupBy(c => c.Key.Mode))
            {
                string? model = run.Models?.FirstOrDefault(m => m.Mode == mode.Key && m.Level == BenchmarkModelLevel.Agent)?.Model;
                var line = (mode.Key, model);
                if (!points.ContainsKey(line)) { lines.Add(line); points[line] = []; }
                var mine = requests.Where(r => r.Turn?.Mode == mode.Key).ToList();
                var costs = mine.Where(r => r.Cost is not null).Select(r => r.Cost!.Value).ToList();
                int right = mode.Count(Right);

                var turns = new List<Turn>();
                int calls = 0, failed = 0;
                foreach (var conversation in mode)
                    foreach (var turn in await store.TurnsAsync(run.Id, conversation.Key, cancellationToken))
                    {
                        turns.Add(turn);
                        var tools = await BenchmarkTools.OfTurnAsync(store, run.Id, turn, cancellationToken);
                        calls += tools.Calls;
                        failed += tools.Failed;
                    }

                long cached = mine.Sum(r => r.CacheReadTokens ?? 0), input = cached + mine.Sum(r => (r.InputTokens ?? 0) + (r.CacheWriteTokens ?? 0));
                var (priced, listed) = mode.Aggregate((0, 0), (sum, c) => (sum.Item1 + Number(c, "priced"), sum.Item2 + Number(c, "ingredients")));
                var answered = mode.Where(Answered).Select(c => c.Key).ToHashSet();
                var answeredCosts = mine.Where(r => r.Turn is not null && answered.Contains(r.Turn.Conversation) && r.Cost is not null).Sum(r => r.Cost!.Value);

                // The runner's rates for this mode, from its last repeat (F18 step 5a): they are cumulative over the repeats, so the last is the mode's.
                var rates = run.Modes?.Where(m => m.Name == mode.Key && m.Rates is not null).OrderBy(m => m.Repeat).LastOrDefault()?.Rates;
                points[line].Add(new Point(run.Id, run.StartedAt,
                    costs.Count == 0 ? null : costs.Sum() / repeats,
                    mode.Any() ? (double)right / mode.Count() : null,
                    mode.Count(),
                    Tokens: mine.Count == 0 ? null : mine.Sum(r => (r.InputTokens ?? 0) + (r.OutputTokens ?? 0) + (r.CacheReadTokens ?? 0) + (r.CacheWriteTokens ?? 0)) / repeats,
                    SecondsPerTurn: turns.Count == 0 ? null : turns.Average(t => t.Seconds),
                    CacheShare: input == 0 ? null : (double)cached / input,
                    Sourced: listed == 0 ? null : (double)priced / listed,
                    ToolsFailed: calls == 0 ? null : (double)failed / calls,
                    CostPerAnswer: answered.Count == 0 || costs.Count == 0 ? null : answeredCosts / answered.Count,
                    Sites: turns.SelectMany(t => t.Hosts).Distinct().Count(),
                    PassAtK: rates?.PassAtK, PassHatK: rates?.PassHatK, K: rates?.K));
            }
        }
        return new Trend(check, lines.Select(l => new Series(SeriesName(l.Mode, l.Model), points[l], l.Model)).ToList());
    }

    private static bool Right(Conversation c) =>
        c.Scores.ValueKind == JsonValueKind.Object && c.Scores.TryGetProperty("routed_right", out var v) && v.ValueKind == JsonValueKind.True;

    private static int Number(Conversation c, string name) =>
        c.Scores.ValueKind == JsonValueKind.Object && c.Scores.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : 0;

    /// <summary>A real answer: routed right, and a meal at least <see cref="AnsweredShare"/> priced with sources (a meal without a list isn't one).</summary>
    private static bool Answered(Conversation c)
    {
        if (!Right(c)) return false;
        if (!c.Scores.TryGetProperty("ingredients", out var n)) return true;
        if (n.ValueKind != JsonValueKind.Number) return !c.Key.Task.StartsWith("cook", StringComparison.Ordinal);
        return c.Scores.TryGetProperty("priced_ratio", out var r) && r.ValueKind == JsonValueKind.Number && r.GetDouble() >= AnsweredShare;
    }
}
