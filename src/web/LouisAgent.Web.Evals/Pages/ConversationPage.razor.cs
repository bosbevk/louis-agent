using System.Net;
using LouisAgent.Evals.Contracts.Api;
using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Evals.Contracts.Runs.Answers;
using LouisAgent.Web.Api;
using LouisAgent.Web.Evals.Client;
using LouisAgent.Web.Evals.Components.Marks;
using LouisAgent.Web.Evals.Words;
using LouisAgent.Web.Evals.Words.Parts;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Evals.Pages;

/// <summary>
/// A conversation's page. It reads the run whole, picks the conversation and the repeat from the address, and draws
/// that repeat turn by turn. Moving to another repeat of the same run changes only the address, so it reads again
/// only when the run changes, on Reload, or after a person flips a mark.
/// </summary>
/// <remarks>
/// Which repeat opens is the conversation's own answer (<see cref="ConversationRun.FindRepeat" />, ADR 0123), so a
/// link with no repeat in it opens the first failed one, the one worth reading first.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0107-every-section-has-an-address.md">ADR 0107: every section has an address</seealso>
public partial class ConversationPage
{
    private EvalRun? _run;
    private IReadOnlyList<RunRow> _runs = [];
    private string? _read, _error, _refused;
    private bool _needsKey, _loading, _missing;

    /// <summary>The eval the run belongs to, from the address.</summary>
    [Parameter]
    public string Eval { get; set; } = "";

    /// <summary>The run's id, from the address.</summary>
    [Parameter]
    public string Run { get; set; } = "";

    /// <summary>The conversation's id, from the address.</summary>
    [Parameter]
    public string Conversation { get; set; } = "";

    /// <summary>The repeat asked for, the address's last part, or none.</summary>
    [Parameter]
    public int? Repeat { get; set; }

    [Inject] private EvalsApi Api { get; set; } = null!;
    [Inject] private ChatStore Store { get; set; } = null!;

    /// <summary>The conversation the address names, once the run is read.</summary>
    private ConversationRun? Held => _run?.Conversations.FirstOrDefault(c => c.Id == Conversation);

    /// <summary>The repeat on show.</summary>
    private RepeatRun? Open => Held?.FindRepeat(Repeat);

    /// <summary>The next failed conversation, in the run page's order, failed first.</summary>
    private ConversationRun? Next => _run?.FindNextFailed(Conversation);

    /// <summary>The conversation before this one, in the same order.</summary>
    private ConversationRun? Previous => _run?.FindPrevious(Conversation);

    /// <summary>The eval's runs, asked which is the run before and how often a criterion was flipped lately.</summary>
    private RunHistory History => new(_runs);

    /// <summary>
    /// The run before this one, by the comparison's rule (F126.4): the run a re-grade re-graded, else the newest
    /// earlier run that finished. The list doesn't say which conversations a run holds, so the link isn't checked
    /// ahead; that run's page says so when it lacks this one.
    /// </summary>
    private RunRow? Before => _runs.FirstOrDefault(row => row.Id == Run) is { } row ? History.FindBefore(row) : null;

    private string RunPage => EvalAddress.RunPageOf(Eval, Run);

    private string PageOf(ConversationRun conversation) => EvalAddress.ConversationPageOf(Eval, Run, conversation.Id);

    private string BeforePageOf(RunRow before) => EvalAddress.ConversationPageOf(Eval, before.Id, Conversation);

    private string Repeats =>
        EvalsWords.Count(Held?.Asked ?? 0, EvalsPart.Repeat, EvalsPart.Repeats);

    private static string Word(EvalsPart part) => EvalsWords.Words[part];

    private static string MetOf(MarkCount count) => EvalsWords.Fill(EvalsPart.MetOf, count.Met, count.Of);

    /// <summary>"Passed after your flip. 3 of 3 repeats passed. This is repeat 1.", asked of its records.</summary>
    private string HeadOf(ConversationRun held, RepeatRun open)
    {
        var verdict = held switch
        {
            _ when held.PassedAfterAFlip() => EvalsPart.PassedAfterYourFlip,
            { Passed: true } => EvalsPart.Passed,
            _ => EvalsPart.Failed
        };
        return EvalsWords.Fill(EvalsPart.ConversationHead, Word(verdict), held.CountPassedRepeats(), Repeats,
            open.Number);
    }

    /// <summary>Sends a person's flip of a criterion, then reads the run again; a refusal is a line.</summary>
    private Task FlipAsync(FlipAsk ask) => ChangeAsync(() => Api.FlipAsync(ask.Place, ask.Note));

    /// <summary>Puts the judge's answer back on a flipped mark, then reads the run again.</summary>
    private Task UndoFlipAsync(MarkPlace place) => ChangeAsync(() => Api.UndoFlipAsync(place));

    /// <summary>
    /// A change to the run: sent, then the run read again, so the page shows what the run keeps (ADR 0042). A refusal
    /// is a line in the API's words, and a refused key shows the frame's key box, as a read does.
    /// </summary>
    private async Task ChangeAsync(Func<Task<string?>> change)
    {
        try
        {
            _refused = await change();
        }
        catch (AgentApiException ex)
        {
            _needsKey = ex.Status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;
            _error = ex.Message;
            return;
        }

        await LoadAsync();
    }

    /// <summary>What this repeat's turns cost the agent, or unknown when none was priced, never 0 (ADR 0007).</summary>
    private string Cost => Open?.SumCost() is { } cost
        ? UsageFormat.Money(cost, _run?.FindCurrency())
        : EvalsWords.Words[EvalsPart.Unknown];

    /// <summary>Loads the admin key, then reads the run when the address names one not read yet.</summary>
    protected override async Task OnParametersSetAsync()
    {
        if (!Store.Loaded) await Store.LoadAsync();
        if (_read != Run) await LoadAsync();
    }

    /// <summary>
    /// Reads the run and the eval's runs. A run the host doesn't have is one line; a refused key shows the frame's
    /// key box, and a host that can't be reached says so in a line.
    /// </summary>
    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        try
        {
            _run = await Api.FindRunAsync(Run);
            _missing = _run is null;
            _runs = await Api.ListRunsAsync(Eval);
            _read = Run;
        }
        catch (AgentApiException ex)
        {
            _needsKey = ex.Status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;
            _error = ex.Message;
        }
        catch (HttpRequestException ex)
        {
            _error = ex.Message;
        }

        _loading = false;
    }
}
