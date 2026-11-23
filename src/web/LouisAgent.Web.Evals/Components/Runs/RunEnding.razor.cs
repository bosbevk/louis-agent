using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Web.Evals.Words;
using LouisAgent.Web.Evals.Words.Parts;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Evals.Components.Runs;

/// <summary>
/// How a run that didn't finish ended, said before its figures. A person's stop is no failure, so it is drawn in the
/// page's own colours; a run that could not finish is the harness failing, drawn as an error with the run's reason.
/// A finished run, or one still going, has no ending to tell.
/// </summary>
public partial class RunEnding
{
    /// <summary>The run whose ending is told.</summary>
    [Parameter, EditorRequired]
    public EvalRun Run { get; set; } = null!;

    /// <summary>How many of the eval's conversations it didn't reach, in words: "4 conversations".</summary>
    [Parameter, EditorRequired]
    public string NotRun { get; set; } = "";

    private static string StoppedTitle => EvalsWords.Words[EvalsPart.StoppedTitle];

    private static string StoppedByAPerson => EvalsWords.Words[EvalsPart.StoppedByAPerson];

    private string DidNotRun => EvalsWords.Fill(EvalsPart.DidNotRun, NotRun);
}
