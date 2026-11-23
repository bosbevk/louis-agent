using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Evals.Contracts.Runs.Answers;

namespace LouisAgent.Evals.Contracts.Api;

/// <summary>One eval this host has: its name, the agent it proves, how many conversations, and its last run.</summary>
/// <param name="Name">The eval's name, its file's name without <c>.eval.json</c>.</param>
/// <param name="Agent">The address of the agent it proves.</param>
/// <param name="Conversations">How many conversations its file holds.</param>
/// <param name="LastRun">Its newest run in short, or null before its first.</param>
/// <param name="LastLiveRun">Its newest live run that finished, the one a replay plays back, or null.</param>
public sealed record EvalRow(string Name, string Agent, int Conversations, RunRow? LastRun, RunRow? LastLiveRun = null)
{
    /// <summary>The run its last run is compared with (<see cref="RunHistory.FindBefore" />), or none.</summary>
    public RunRow? RunBefore { get; init; }

    /// <summary>How many more conversations its last run passed than the run before; none without one.</summary>
    public int? CountChange() => LastRun?.Passed - RunBefore?.Passed;

    /// <summary>
    /// What a live run of this many repeats is likely to cost: what the last live run spent a repeat, the agent's and
    /// the judge's added, times the repeats. None with no live run, or when that run had no price, never 0. When only
    /// one of the two costs was known, it is that one alone, as every sum of a run adds what is known
    /// (<see cref="KnownCost" />), so it says less than the run will spend rather than nothing.
    /// </summary>
    public decimal? EstimateLive(int repeats) =>
        LastLiveRun is { Repeats: > 0 } live && KnownCost.Sum([live.Cost, live.JudgeCost]) is { } spent
            ? spent / live.Repeats * repeats
            : null;

    /// <summary>Whether it has a live run that finished, the one a replay plays back for free.</summary>
    public bool CanReplay() => LastLiveRun is not null;

    /// <summary>
    /// Its last run finished and every one of its conversations passed. A method, so the API's answer gains no field.
    /// </summary>
    public bool LastRunPassedInFull() =>
        LastRun is { Status: RunStatus.Finished } run && run.Passed == Conversations;

    /// <summary>
    /// How many of its conversations a run of it didn't reach: a run keeps only those it graded, so a run that
    /// stopped says how many, not which.
    /// </summary>
    public int CountNotRunIn(EvalRun run) => Math.Max(0, Conversations - run.Conversations.Count);
}
