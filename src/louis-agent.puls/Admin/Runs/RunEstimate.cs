namespace LouisAgent.Puls.Admin.Runs;

/// <summary>
/// What the Benchmarks page works out before a run (the rework of 2026-10-26): what a run will roughly cost, and which check to show first (a check is selected on arrival, so the list of runs is never empty). The estimate is the last
/// run that measured the agent (done, not a re-score, which asked nothing) divided by its repeats and scaled to the repeats chosen; null when no such run has a known cost,
/// never 0 (ADR 0007). The default check is the one with the newest run, so the list of runs is never empty on arrival; the first check when none has run.
/// </summary>
public static class RunEstimate
{
    public static decimal? For(string check, IReadOnlyList<RunSummary> runs, int repeats)
    {
        var measured = runs.Where(run => run.Check == check && run.Status == "done" && run.RescoreOf is null && run.Total?.Cost is not null)
            .OrderByDescending(run => run.StartedAt).FirstOrDefault();
        return measured is null ? null : measured.Total!.Cost!.Value / Math.Max(1, measured.Repeat) * repeats;
    }

    /// <summary>The line beside the Run button: "free" for the stub, else the estimate at the repeats chosen, in the page's money; empty when no cost is known.</summary>
    public static string Text(string check, IReadOnlyList<RunSummary> runs, int repeats, Func<decimal, string> money) =>
        check == "stub" ? "free"
        : For(check, runs, repeats) is { } cost ? $"≈ {money(cost)} for {repeats} {(repeats == 1 ? "repeat" : "repeats")}" : "";

    public static string? DefaultCheck(IReadOnlyList<string> checks, IReadOnlyList<RunSummary> runs) =>
        runs.OrderByDescending(run => run.StartedAt).Select(run => run.Check).FirstOrDefault(check => checks.Contains(check)) ?? checks.FirstOrDefault();
}
