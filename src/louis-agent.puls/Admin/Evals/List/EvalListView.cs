using LouisAgent.Api.Contracts.Admin.Evals;
using LouisAgent.Puls.Admin.Rates;

namespace LouisAgent.Puls.Admin.Evals.List;

/// <summary>
/// The Evals page's list, worked out from the list reply before any markup: a line per eval with its last run and
/// per mode the pass@k and its change against the mode's run before, and which eval opens when the address names
/// none (the one that ran last, else the first).
/// </summary>
/// <remarks>
/// Presentation Model (Fowler), as the run page's verdict: the list only has to say enough to choose an eval, so
/// its numbers are a plain record with tests and the component places them. A mode's run before is the newest
/// earlier run that has rates for that mode, so a mode added later still compares with its own past.
/// </remarks>
public sealed record EvalListView(IReadOnlyList<EvalLine> Evals)
{
    /// <summary>A line per eval, in the catalogue's order.</summary>
    public static EvalListView Of(EvalListReply list) => new([.. list.Evals.Select(Line)]);

    /// <summary>
    /// The eval to open when the address names none: the one whose newest run started last, else the first.
    /// </summary>
    public static string? DefaultEval(EvalListReply list) =>
        list.Evals.Where(e => e.Runs.Count > 0).OrderByDescending(e => e.Runs[0].Run.StartedAt)
            .Select(e => e.Name).FirstOrDefault() ?? list.Evals.Select(e => e.Name).FirstOrDefault();

    /// <summary>One eval's line: its newest run, and each of that run's modes against the mode's run before.</summary>
    private static EvalLine Line(EvalRow eval)
    {
        var latest = eval.Runs.FirstOrDefault();
        if (latest is null) return new EvalLine(eval.Name, eval.Proves, null, null, [], eval.NeverRun, 0);

        var modes = ModeRatesReader.Read(latest.Modes)
            .Select(rate => new ModeChange(rate.Mode, rate.K, rate.PassAtK, rate.PassAtK - Before(eval, rate.Mode)));
        return new EvalLine(eval.Name, eval.Proves, latest.Run.Status, latest.Run.StartedAt, [.. modes],
            eval.NeverRun, eval.Runs.Count);
    }

    /// <summary>The mode's pass@k on the newest run before the latest that has it; none without such a run.</summary>
    private static double? Before(EvalRow eval, string mode) =>
        eval.Runs.Skip(1).SelectMany(run => ModeRatesReader.Read(run.Modes))
            .FirstOrDefault(rate => rate.Mode == mode && rate.PassAtK is not null)?.PassAtK;
}
