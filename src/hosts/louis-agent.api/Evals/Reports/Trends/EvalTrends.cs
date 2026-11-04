using LouisAgent.Api.Evals.Reports.Prompts;
using LouisAgent.Api.Evals.Runs.Kinds;
using LouisAgent.Api.Evals.Runs.Start;
using LouisAgent.Api.Evals.Store;
using LouisAgent.Evals.Protocol.Kinds;
using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Evals.Reports.Trends;

/// <summary>
/// A check's trend over its runs, per mode, a point per finished run, oldest first. What one repeat cost and its
/// tokens, seconds per turn, the share of conversations that passed, the share of input from the cache, the share of
/// tool calls refused, what one passed conversation cost, and how many sites were read. Each point also carries the
/// notes written on its run and the agents whose prompts changed since the point before, so you can see why a line
/// moved. A run still going isn't a point: its cost isn't complete. A value nobody measured is null, never 0; the
/// share of ingredients priced with a source is always null now, since no grader measures it.
/// </summary>
/// <remarks>
/// Computed in C# over the store, so it's the same on the in-memory store as on MongoDB. The score comes from each
/// conversation's outcome, not an eval's own keys, so the trend works for every eval (Louis's pick, F18 step 7c.3).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0007-measure-every-request.md">ADR 0007: measure every request</seealso>
public static class EvalTrends
{
    /// <summary>
    /// One run in one mode, with the notes on it and the agents whose prompts changed since the point before.
    /// </summary>
    public sealed record Point(
        string Run,
        DateTimeOffset At,
        decimal? Cost,
        double? Score,
        int Conversations,
        long? Tokens = null,
        double? SecondsPerTurn = null,
        double? CacheShare = null,
        double? Sourced = null,
        double? ToolsFailed = null,
        decimal? CostPerAnswer = null,
        int? Sites = null,
        double? PassAtK = null,
        double? PassHatK = null,
        int? K = null,
        IReadOnlyList<string>? Notes = null,
        IReadOnlyList<string>? PromptChanged = null);

    /// <summary>
    /// One line of the trend: a mode on one model. Its name is the mode, with its model in brackets when the model is
    /// named.
    /// </summary>
    public sealed record Series(string Name, IReadOnlyList<Point> Points, string? Model = null);

    /// <summary>A check's trend: a line per mode and model.</summary>
    public sealed record Trend(string Check, IReadOnlyList<Series> Modes);

    /// <summary>The line's name: the mode, and its model in brackets when we know it.</summary>
    private static string SeriesName(string mode, string? model) => model is null ? mode : $"{mode} ({model})";

    /// <summary>The trend over the check's last runs, a line per mode and model.</summary>
    public static async Task<Trend> ComputeAsync(IEvalStore store, string check, int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var runs = await FinishedRunsAsync(store, check, limit, cancellationToken);
        var byId = runs.ToDictionary(r => r.Id);

        // A line is a mode on one model: runs on two models of one mode are two lines, never one line joining them.
        var lines = new List<(string Mode, string? Model)>();
        var points = new Dictionary<(string Mode, string? Model), List<Point>>();
        foreach (var run in runs)
        {
            var conversations = await store.ConversationsAsync(run.Id, cancellationToken);
            var requests = await store.RequestsAsync(run.Id, cancellationToken);
            var notes = await store.NotesAsync(run.Id, cancellationToken);
            int repeats = Math.Max(1, run.Repeat);
            foreach (var mode in conversations.GroupBy(c => c.Key.Mode))
            {
                string? model = run.Models
                    ?.FirstOrDefault(m => m.Mode == mode.Key && m.Level == ModelLevel.Agent)?.Model;
                var line = (mode.Key, model);
                if (!points.ContainsKey(line))
                {
                    lines.Add(line);
                    points[line] = [];
                }

                var mine = requests.Where(r => r.Turn?.Mode == mode.Key).ToList();
                var costs = mine.Where(r => r.Cost is not null).Select(r => r.Cost!.Value).ToList();
                // The conversations that count, as pass@k counts them: an infrastructure failure is neither a pass
                // nor a fail, and one still without an outcome isn't graded yet.
                var counted = mode.Where(c => c.Outcome is not (null or TrialOutcome.Infrastructure)).ToList();
                var passed = counted.Where(c => c.Outcome == TrialOutcome.Pass).Select(c => c.Key).ToHashSet();

                var turns = new List<Turn>();
                int calls = 0, failed = 0;
                foreach (var conversation in mode)
                    foreach (var turn in await store.TurnsAsync(run.Id, conversation.Key, cancellationToken))
                    {
                        turns.Add(turn);
                        var tools = await EvalTools.OfTurnAsync(store, run.Id, turn, cancellationToken);
                        calls += tools.Calls;
                        failed += tools.Failed;
                    }

                long cached = mine.Sum(r => r.CacheReadTokens ?? 0),
                    input = cached + mine.Sum(r => (r.InputTokens ?? 0) + (r.CacheWriteTokens ?? 0));
                // What the passed conversations cost: an answer is a conversation that passed.
                var passedCosts =
                    mine.Where(r => r.Turn is not null && passed.Contains(r.Turn.Conversation) && r.Cost is not null)
                        .Sum(r => r.Cost!.Value);

                // The runner's rates for this mode, from its last repeat: they are cumulative over the repeats, so the
                // last is the mode's.
                var rates = run.Modes?.Where(m => m.Name == mode.Key && m.Rates is not null).OrderBy(m => m.Repeat)
                    .LastOrDefault()?.Rates;
                // The point before on this line, whose prompts this run's are compared with, agent by agent.
                var before = points[line].LastOrDefault() is { } last ? byId[last.Run] : null;
                points[line].Add(new Point(run.Id, run.StartedAt,
                    costs.Count == 0 ? null : costs.Sum() / repeats,
                    counted.Count == 0 ? null : (double)passed.Count / counted.Count,
                    mode.Count(),
                    Tokens: mine.Count == 0
                        ? null
                        : mine.Sum(r =>
                            (r.InputTokens ?? 0) + (r.OutputTokens ?? 0) + (r.CacheReadTokens ?? 0) +
                            (r.CacheWriteTokens ?? 0)) / repeats,
                    SecondsPerTurn: turns.Count == 0 ? null : turns.Average(t => t.Seconds),
                    CacheShare: input == 0 ? null : (double)cached / input,
                    ToolsFailed: calls == 0 ? null : (double)failed / calls,
                    CostPerAnswer: passed.Count == 0 || costs.Count == 0 ? null : passedCosts / passed.Count,
                    Sites: turns.SelectMany(t => t.Hosts).Distinct().Count(),
                    PassAtK: rates?.PassAtK, PassHatK: rates?.PassHatK, K: rates?.K,
                    Notes: NotesOf(notes, mode.Key),
                    PromptChanged: before is null
                        ? null
                        : AgentPrompts.Changed(before.Prompts, run.Prompts, mode.Key)));
            }
        }

        return new Trend(check,
            lines.Select(l => new Series(SeriesName(l.Mode, l.Model), points[l], l.Model)).ToList());
    }

    /// <summary>
    /// The runs that are points of a check's trends, oldest first (the overall trend's and the categories'): the
    /// finished dev runs that measured the agent. A replay and a re-score are left out (the recorded run again, and old
    /// transcripts graded again), a held-out run or one of all of them is a different set of conversations, and a run
    /// still going has an incomplete cost.
    /// </summary>
    internal static async Task<List<Run>> FinishedRunsAsync(IEvalStore store, string check, int limit,
        CancellationToken cancellationToken)
    {
        // One run at a time (the runner's rule), so one more than the limit covers a run still going.
        return (await store.ListRunsAsync(check, limit + 1, cancellationToken))
            .Where(r => r.Status != EvalRunStatus.Running)
            // The trend is the dev scenarios: a held-out run, or one of all of them, is a different set of
            // conversations, and a run before splits were kept (no split) was a dev run. Joining them would draw a
            // line through two different things.
            .Where(r => r.Split is null or RunSplit.Dev)
            // A replay is the recorded run again: its scores are that run's and it costs nothing, so it is no point.
            .Where(r => EvalSettings.ReplayOf(r.Settings) is null)
            // A re-score measures nothing new: it grades transcripts recorded on another day, so a point would put
            // their quality at its date and its $0 on the cost line.
            .Where(r => r.RescoreOf is null)
            .Take(limit).Reverse().ToList();
    }

    /// <summary>
    /// What was written about a run in one mode: the notes on the run itself and on this mode's conversations, in the
    /// order they were written (the unattended run's finding 15).
    /// </summary>
    private static IReadOnlyList<string> NotesOf(IReadOnlyList<RunNote> notes, string mode)
    {
        var texts = notes.Where(n => n.Conversation is null || n.Conversation.Mode == mode).Select(n => n.Text)
            .ToArray();
        // The same empty list every time, so two points without notes are equal records.
        return texts.Length == 0 ? [] : texts;
    }
}
