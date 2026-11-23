using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Evals.Contracts.Runs.Answers;
using LouisAgent.Web.Evals.Words;
using LouisAgent.Web.Evals.Words.Parts;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Evals.Components.Changes;

/// <summary>
/// One line under a run's heading: how many more or fewer conversations passed than the run before, which run that
/// was, and which conversations now pass and now fail. The change is the run's own answer
/// (<see cref="EvalRun.CompareWith" />, ADR 0123), in the theme's green when better and red when worse.
/// </summary>
/// <remarks>
/// Compared with the run before, as <c>RunHistory</c> finds it (Louis's pick, 2026-11-23, "Run before; re-grade vs
/// its own"), the way Braintrust colour-codes a comparison: green for an improvement, red for a regression.
/// </remarks>
public partial class RunChangeLine
{
    /// <summary>The eval the runs belong to.</summary>
    [Parameter, EditorRequired]
    public string Eval { get; set; } = "";

    /// <summary>The run, against the one before, or none with no run before.</summary>
    [Parameter]
    public RunChange? Change { get; set; }

    /// <summary>The run before, read whole, or none.</summary>
    [Parameter]
    public EvalRun? Before { get; set; }

    /// <summary>Better in the theme's green, worse in its red, the same in the body's colour.</summary>
    private static string CountClass(RunChange change) => (change.PassedNow - change.PassedBefore) switch
    {
        > 0 => "is-better",
        < 0 => "is-worse",
        _ => ""
    };

    /// <summary>" · guide-bank now passes · guide-pricing now fails", each part only when it names one.</summary>
    private static string Named(RunChange change)
    {
        var parts = new List<string>();
        if (change.NowPass.Count > 0)
            parts.Add(EvalsWords.Fill(EvalsPart.NowPasses, string.Join(", ", change.NowPass)));
        if (change.NowFail.Count > 0) parts.Add(EvalsWords.Fill(EvalsPart.NowFails, string.Join(", ", change.NowFail)));
        return string.Concat(parts.Select(part => " · " + part));
    }
}
