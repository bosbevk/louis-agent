using LouisAgent.Choices;
using LouisAgent.Evals.Contracts.Api;
using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Web.Api;
using LouisAgent.Web.Evals.Client;
using LouisAgent.Web.Evals.Words;
using LouisAgent.Web.Evals.Words.Parts;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Evals.Components.Evals;

/// <summary>
/// One eval's card: how its last run went, and the two ways to start the next. A replay is free and starts at once; a
/// live run spends money, so it opens the ask. After a start the browser goes to the run's page; a refusal is shown
/// on the card in the host's own words. While the eval's run goes, the card says since when and links the run.
/// </summary>
/// <remarks>
/// Which card's ask is open is the page's to hold, so one is open at a time (Louis's pick, 2026-11-23, over each card
/// holding its own and over a shared state): the card is told <see cref="AskOpen" /> and asks with
/// <see cref="OnAsk" />, state down and events up. Its buttons are off from a click until the answer, so a double
/// click starts one run; the host's 409 stays the guard for a run started elsewhere.
/// </remarks>
public partial class EvalCard
{
    private string? _refused;
    private bool _starting;

    /// <summary>The eval's row, with its last run and its last live run.</summary>
    [Parameter, EditorRequired]
    public EvalRow Row { get; set; } = null!;

    /// <summary>Whether this card's ask is the one open.</summary>
    [Parameter]
    public bool AskOpen { get; set; }

    /// <summary>Whether another eval's run is going, so this card waits.</summary>
    [Parameter]
    public bool Waiting { get; set; }

    /// <summary>Asks the page to open this card's ask, with its name, or to close it, with none.</summary>
    [Parameter]
    public EventCallback<string?> OnAsk { get; set; }

    [Inject] private EvalsApi Api { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private string ConversationCount =>
        EvalsWords.Count(Row.Conversations, EvalsPart.Conversation, EvalsPart.Conversations);

    private static string NotRunYet => EvalsWords.Words[EvalsPart.NotRunYet];

    private static string CouldNotFinish => EvalsWords.Words[EvalsPart.CouldNotFinish];

    private static string Stopped => EvalsWords.Words[EvalsPart.Stopped];

    private static string ReplayFree => EvalsWords.Words[EvalsPart.ReplayFree];

    private static string RunLiveAsk => EvalsWords.Words[EvalsPart.RunLiveAsk];

    private static string NothingToReplay => EvalsWords.Words[EvalsPart.NothingToReplay];

    private static string FollowTheRun => EvalsWords.Words[EvalsPart.FollowTheRun];

    private bool CouldNotFinishThen => Row.LastRun is { Status: RunStatus.CouldNotFinish };

    private bool StoppedThen => Row.LastRun is { Status: RunStatus.Stopped };

    /// <summary>Whether this eval's run is going now.</summary>
    private bool Going => Row.LastRun is { Status: RunStatus.Running };

    /// <summary>The buttons are off while another run goes, and from a click until its answer.</summary>
    private bool Off => Waiting || _starting;

    private string When => Row.LastRun is { } run ? EvalsWords.FormatLocal(run.StartedAt) : "";

    private string GoingSince => EvalsWords.Fill(EvalsPart.GoingSince, When);

    private string Kind => Row.LastRun?.Kind.Wire() ?? "";

    private string RepeatCount => EvalsWords.Count(Row.LastRun?.Repeats ?? 0, EvalsPart.Repeat, EvalsPart.Repeats);

    private string Open => Row.LastRun is { } run ? EvalAddress.RunPageOf(Row.Name, run.Id) : "";

    private Task AskAsync() => OnAsk.InvokeAsync(Row.Name);

    private Task CloseAsync() => OnAsk.InvokeAsync(null);

    /// <summary>A replay plays the last live run back with its own repeats, which are one or three.</summary>
    private Task ReplayAsync() => StartAsync(RunKind.Replay, Row.LastLiveRun?.Repeats ?? 1);

    private Task RunLiveAsync(int repeats) => StartAsync(RunKind.Live, repeats);

    /// <summary>Starts a run and goes to its page; a refusal stays on the card in the host's own words.</summary>
    private async Task StartAsync(RunKind kind, int repeats)
    {
        if (_starting) return;
        _starting = true;
        _refused = null;
        try
        {
            var start = await Api.StartAsync(Row.Name, kind, repeats);
            if (start.Run is { } run)
            {
                Navigation.NavigateTo(EvalAddress.RunPageOf(Row.Name, run));
                return;
            }

            _refused = start.Refused;
        }
        catch (AgentApiException refused)
        {
            _refused = refused.Message;
        }

        _starting = false;
    }
}
