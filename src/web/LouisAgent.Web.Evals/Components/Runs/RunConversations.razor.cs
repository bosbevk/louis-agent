using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Evals.Contracts.Runs.Answers;
using LouisAgent.Web.Evals.Words;
using LouisAgent.Web.Evals.Words.Parts;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Evals.Components.Runs;

/// <summary>
/// A run's conversations in the run's own order (<see cref="EvalRun.OrderFailedFirst" />), each a row, told what now
/// passes and now fails against the run before.
/// </summary>
public partial class RunConversations
{
    /// <summary>The eval the run belongs to.</summary>
    [Parameter, EditorRequired]
    public string Eval { get; set; } = "";

    /// <summary>The run.</summary>
    [Parameter, EditorRequired]
    public EvalRun Run { get; set; } = null!;

    /// <summary>The run against the one before, or none.</summary>
    [Parameter]
    public RunChange? Change { get; set; }

    private static string Title => EvalsWords.Words[EvalsPart.ConversationsTitle];
}
