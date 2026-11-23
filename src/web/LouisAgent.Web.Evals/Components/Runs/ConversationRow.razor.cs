using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Evals.Contracts.Runs.Answers;
using LouisAgent.Web.Evals.Client;
using LouisAgent.Web.Evals.Words;
using LouisAgent.Web.Evals.Words.Parts;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Evals.Components.Runs;

/// <summary>
/// A conversation in a run, one line of the run's page: whether it passed, what it first asked, and for a failed one
/// which repeat failed and on what, in the first miss's own words.
/// </summary>
public partial class ConversationRow
{
    /// <summary>The eval the run belongs to.</summary>
    [Parameter, EditorRequired]
    public string Eval { get; set; } = "";

    /// <summary>The run's id.</summary>
    [Parameter, EditorRequired]
    public string Run { get; set; } = "";

    /// <summary>The conversation this row draws.</summary>
    [Parameter, EditorRequired]
    public ConversationRun Conversation { get; set; } = null!;

    /// <summary>The run against the one before, so a row says it now passes or now fails (F126.4); or none.</summary>
    [Parameter]
    public RunChange? Change { get; set; }

    private bool NowPasses => Change?.NowPass.Contains(Conversation.Id) == true;

    private bool NowFails => Change?.NowFail.Contains(Conversation.Id) == true;

    private static string NowPassesTag => EvalsWords.Words[EvalsPart.NowPassesTag];

    private static string NowFailsTag => EvalsWords.Words[EvalsPart.NowFailsTag];

    private string Open => EvalAddress.ConversationPageOf(Eval, Run, Conversation.Id);

    private string? Ask => Conversation.FindFirstAsk();

    private RepeatRun? FailedRepeat => Conversation.FindFirstFailedRepeat();

    private Miss? FirstMiss => FailedRepeat?.FindFirstMiss();

    /// <summary>"fact not met" for a fact, "not met" for a criterion, whose own words say what it asked.</summary>
    private string MissWords =>
        EvalsWords.Words[FirstMiss?.Kind == MarkKind.Fact ? EvalsPart.FactNotMet : EvalsPart.NotMet];

    private static string PassedAfterYourFlip => EvalsWords.Words[EvalsPart.PassedAfterYourFlip];

    private string Repeats => EvalsWords.Count(Conversation.Asked, EvalsPart.Repeat, EvalsPart.Repeats);
}
