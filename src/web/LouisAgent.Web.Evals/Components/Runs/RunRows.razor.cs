using LouisAgent.Choices;
using LouisAgent.Evals.Contracts.Api;
using LouisAgent.Web.Evals.Client;
using LouisAgent.Web.Evals.Words;
using LouisAgent.Web.Evals.Words.Parts;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Evals.Components.Runs;

/// <summary>
/// An eval's runs, where the run before is found. The rows come in the API's order, newest first, and the open one
/// is marked for the eye and for a screen reader.
/// </summary>
public partial class RunRows
{
    /// <summary>The eval the runs belong to.</summary>
    [Parameter, EditorRequired]
    public string Eval { get; set; } = "";

    /// <summary>Its runs, as the API lists them.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<RunRow> Rows { get; set; } = [];

    /// <summary>The run whose page this is.</summary>
    [Parameter, EditorRequired]
    public string Open { get; set; } = "";

    private bool IsOpen(RunRow row) => row.Id == Open;

    private string PageOf(RunRow row) => EvalAddress.RunPageOf(Eval, row.Id);

    private static string KindOf(RunRow row) => row.Kind.Wire();

    private static string RepeatsOf(RunRow row) => EvalsWords.Count(row.Repeats, EvalsPart.Repeat, EvalsPart.Repeats);
}
