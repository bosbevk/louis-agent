using LouisAgent.Evals.Contracts.Api;
using LouisAgent.Web.Evals.Words;
using LouisAgent.Web.Evals.Words.Parts;
using LouisAgent.Web.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Evals.Components.Evals;

/// <summary>
/// The ask before a live run, the one click on these pages that spends money. It says what will be played and what
/// it is likely to cost for the repeats chosen, three to start, and its buttons say what they do: "Run live, about"
/// the money, or "Don't run".
/// </summary>
/// <remarks>
/// A confirmation with the consequence in it and buttons named for their actions, as Nielsen Norman Group advises
/// for an action that costs something. The estimate is asked of the eval's row (ADR 0123), and money is said as
/// everywhere else in the web.
/// </remarks>
/// <seealso href="https://www.nngroup.com/articles/confirmation-dialog/">Nielsen Norman Group: confirmation dialogs</seealso>
public partial class RunAsk
{
    /// <summary>How many repeats are chosen until the person picks: three, a run's full measure.</summary>
    private const int Full = 3;

    private int _repeats = Full;

    /// <summary>The eval to run.</summary>
    [Parameter, EditorRequired]
    public EvalRow Row { get; set; } = null!;

    /// <summary>Whether a start is under way, so the button can't start a second one.</summary>
    [Parameter]
    public bool Starting { get; set; }

    /// <summary>Runs it live, with the repeats chosen.</summary>
    [Parameter]
    public EventCallback<int> OnRun { get; set; }

    /// <summary>Closes the ask, starting nothing.</summary>
    [Parameter]
    public EventCallback OnClose { get; set; }

    private string Question => EvalsWords.Fill(EvalsPart.AskQuestion, Row.Name);

    private string What => EvalsWords.Fill(EvalsPart.AskWhat,
        EvalsWords.Count(Row.Conversations, EvalsPart.Conversation, EvalsPart.Conversations));

    private static string RepeatsLabel => EvalsWords.Words[EvalsPart.RepeatsLabel];

    private static string DontRun => EvalsWords.Words[EvalsPart.DontRun];

    /// <summary>The estimate for the repeats chosen, said as money, or none.</summary>
    private string? Money => Row.EstimateLive(_repeats) is { } estimate
        ? UsageFormat.Money(estimate, Row.LastLiveRun?.Currency)
        : null;

    private string EstimateLine => Money is { } money
        ? EvalsWords.Fill(EvalsPart.Estimate, money, EvalsWords.Count(_repeats, EvalsPart.Repeat, EvalsPart.Repeats))
        : EvalsWords.Words[EvalsPart.CostNotKnown];

    private string Go => Money is { } money
        ? EvalsWords.Fill(EvalsPart.RunLiveAbout, money)
        : EvalsWords.Words[EvalsPart.RunLive];

    private bool IsChosen(int repeats) => repeats == _repeats;

    private string ChoiceClass(int repeats) => IsChosen(repeats) ? "run-ask-option is-chosen" : "run-ask-option";

    private void Choose(int repeats) => _repeats = repeats;

    private Task RunAsync() => OnRun.InvokeAsync(_repeats);
}
