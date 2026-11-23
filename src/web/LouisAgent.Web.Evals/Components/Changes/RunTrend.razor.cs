using LouisAgent.Evals.Contracts.Api;
using LouisAgent.Web.Evals.Words;
using LouisAgent.Web.Evals.Words.Parts;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Evals.Components.Changes;

/// <summary>
/// The last eight finished runs of an eval, a bar each for how many conversations passed, this run marked, and the
/// criteria they missed most with how often a person flipped each. Both are the run history's own answers
/// (<see cref="RunHistory" />), summed from the rows the page already reads.
/// </summary>
/// <remarks>
/// A plain bar in markup over Bootstrap's flex classes and the theme's names, with no chart package. How often a
/// criterion was flipped is said as a count, never as a rate of agreement (the review of 2026-11-22). The tallies
/// come from each run's row (Louis's pick, 2026-11-23, "On each run's row in the list").
/// </remarks>
public partial class RunTrend
{
    /// <summary>The tallest bar, in pixels; a bar is as tall as its share of the eval's conversations.</summary>
    private const int Tallest = 72;

    /// <summary>The eval the runs belong to.</summary>
    [Parameter, EditorRequired]
    public string Eval { get; set; } = "";

    /// <summary>The eval's runs in short.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<RunRow> Rows { get; set; } = [];

    /// <summary>The run on show, whose bar is marked.</summary>
    [Parameter, EditorRequired]
    public string Current { get; set; } = "";

    private RunHistory History => new(Rows);

    private IReadOnlyList<RunRow> Last => History.FindLastFinished(RunHistory.Recent);

    private IReadOnlyList<CriterionTally> Missed => History.TallyMissedMost(RunHistory.Recent);

    private string Title => EvalsWords.Fill(EvalsPart.LastRuns, RunHistory.Recent);

    private static string Note => EvalsWords.Words[EvalsPart.TrendNote];

    private static string MissedMost => EvalsWords.Words[EvalsPart.MissedMost];

    private static string KeepFlipping => EvalsWords.Words[EvalsPart.KeepFlipping];

    /// <summary>A bar's height: its run's share of passed conversations, at least a sliver so 0 still shows.</summary>
    private static int HeightOf(RunRow row) =>
        Math.Max(4, row.Conversations == 0 ? 0 : Tallest * row.Passed / row.Conversations);

    /// <summary>"you flipped it in 2 of 8 · missed in 1 of 8", each part only when it happened.</summary>
    private string HowOf(CriterionTally tally)
    {
        int of = Last.Count;
        var parts = new List<string>();
        if (tally.Flipped > 0) parts.Add(EvalsWords.Fill(EvalsPart.FlippedIn, tally.Flipped, of));
        if (tally.Missed > 0) parts.Add(EvalsWords.Fill(EvalsPart.MissedIn, tally.Missed, of));
        return string.Join(" · ", parts);
    }
}
