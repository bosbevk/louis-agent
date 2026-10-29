using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Puls.Admin.Explorer.Replay;
using LouisAgent.Puls.Admin.Explorer.Tree;
using LouisAgent.Puls.Admin.Grades;
using LouisAgent.Puls.Admin.Notes;
using LouisAgent.Puls.Admin.Runs;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Chats;
using LouisAgent.Web.Shared.Usage;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace LouisAgent.Puls.Components.Admin;

/// <summary>
/// A stored run as charts you drill into, the modes side by side at every level: the run, its conversations, one
/// conversation's turns, one turn, one request. It keeps where you are and the replay of a turn; each level is a
/// component of its own.
/// </summary>
/// <remarks>
/// The run arrives as the contract's <c>StoredRunReply</c>, the page's one read of the API, and is built into a
/// <c>RunTree</c> per repeat, so no part of the explorer picks JSON apart by name; the review panels and the notes
/// get the records they were written for. State flows down as parameters and up as callbacks (the unattended run's
/// finding 8); the replay of a turn stays here, because any move stops it (finding 12).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public partial class RunExplorer : IDisposable
{
    /// <summary>The run we were handed last.</summary>
    private StoredRunReply? _run;

    /// <summary>The shown repeat, built into modes, conversations, turns and requests.</summary>
    private RunTree _tree = RunTree.Empty;

    /// <summary>The graders the run used, for the review panels.</summary>
    private IReadOnlyList<GraderRow>? _graders;

    /// <summary>The run's notes, for the notes panel.</summary>
    private IReadOnlyList<RunNoteEntry> _notes = [];

    /// <summary>The run's id, for the replays and the notes.</summary>
    private string _runId = "";

    /// <summary>How many repeats the run has, and which one is shown.</summary>
    private int _repeats = 1, _repeat = 1;

    /// <summary>The task we last opened from <c>OpenTask</c>, so a redraw doesn't open it again.</summary>
    private string? _opened;

    /// <summary>
    /// Where you are: 0 the modes, 1 the conversations, 2 one conversation, 3 one turn, 4 one request.
    /// </summary>
    private int _level;

    /// <summary>The open conversation's task.</summary>
    private string? _task;

    /// <summary>The open turn's number.</summary>
    private int? _turn;

    /// <summary>The open request.</summary>
    private RequestNode? _request;

    /// <summary>The mode the open request was made in.</summary>
    private ModeNode? _requestMode;

    /// <summary>Which way the next level slides: in going deeper, out going back.</summary>
    private string _dir = "in";

    /// <summary>The list shows only the conversations still to review.</summary>
    private bool _onlyToReview;

    /// <summary>The open turn replayed, one per mode.</summary>
    private readonly Dictionary<string, Turn> _replays = [];

    /// <summary>A replay is playing.</summary>
    private bool _replaying;

    /// <summary>Stops the replay that is playing.</summary>
    private CancellationTokenSource? _replayStop;

    /// <summary>The chats side by side are open.</summary>
    private bool _chatOpen;

    /// <summary>The conversation the chats opened on.</summary>
    private string? _chatTask;

    /// <summary>Counts each opening of the chats, so they play again from the start.</summary>
    private int _chatPlay;

    /// <summary>The API, to fetch a turn's stored events.</summary>
    [Inject]
    private AgentApi Api { get; set; } = null!;

    /// <summary>The page's scripts, which make the cost numbers count up.</summary>
    [Inject]
    private IJSRuntime Js { get; set; } = null!;

    /// <summary>The stored run, as the API sent it.</summary>
    [Parameter, EditorRequired]
    public StoredRunReply Run { get; set; } = null!;

    /// <summary>The currency to show money in.</summary>
    [Parameter]
    public CurrencyChoice? Currency { get; set; }

    /// <summary>A task to open at once, when a category's line sends you here.</summary>
    [Parameter]
    public string? OpenTask { get; set; }

    /// <summary>A key for the level and whatever is open in it, so a new level slides in.</summary>
    private string LevelKey => $"{_repeat}|{_level}|{_task}|{_turn}|{_request?.At:O}";

    /// <summary>Takes the run, keeps the repeat in range, builds the tree, and opens <c>OpenTask</c> once.</summary>
    protected override void OnParametersSet()
    {
        _run = Run;
        _runId = _run.Run.Id;
        _repeats = Math.Max(1, _run.Run.Repeat);
        _graders = _run.Run.Graders?.Select(GraderRow.Of).ToList();
        _notes = NoteReader.Of(_runId, _run.Notes);

        // Keep the shown repeat inside what this run has.
        _repeat = Math.Clamp(_repeat, 1, _repeats);
        _tree = RunTree.Of(_run, _repeat);

        // Open the task we were sent to, once (a redraw must not open it again).
        if (OpenTask is null || OpenTask == _opened) return;
        _opened = OpenTask;
        OpenConversation(OpenTask);
    }

    /// <summary>After each draw, makes the cost numbers count up.</summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // The landing page's pulsUi.countCosts, keyed on data-count; without the page's script nothing counts.
        try
        {
            await Js.InvokeVoidAsync("pulsUi.countCosts");
        }
        catch (JSException)
        {
        }
    }

    /// <summary>Stops a replay that is still playing.</summary>
    public void Dispose() => _replayStop?.Cancel();

    /// <summary>Switches to another repeat, at the same depth if that conversation or turn is there too.</summary>
    private void ShowRepeat(int repeat)
    {
        _repeat = repeat;
        _tree = RunTree.Of(_run!, _repeat);

        int to = RepeatSwitch.LevelAfter(_level, _task, _turn, _tree.Shape);
        Go(to, stay: to > 0);
    }

    /// <summary>Goes one level deeper: stops any replay, then does what <paramref name="set"/> says.</summary>
    private void Open(Action set)
    {
        _dir = "in";
        ResetReplay();
        set();
    }

    /// <summary>Opens the conversations.</summary>
    private void OpenConversations() => Open(() => _level = 1);

    /// <summary>Opens a conversation by its task.</summary>
    private void OpenConversation(string task) => Open(() =>
    {
        _level = 2;
        _task = task;
    });

    /// <summary>Opens a turn of the open conversation.</summary>
    private void OpenTurn(int n) => Open(() =>
    {
        _level = 3;
        _turn = n;
    });

    /// <summary>Opens a request of the open turn, in the mode it was made in.</summary>
    private void OpenRequest((RequestNode Request, ModeNode Mode) pick) => Open(() =>
    {
        _level = 4;
        _request = pick.Request;
        _requestMode = pick.Mode;
    });

    /// <summary>Goes back to a level from the crumbs.</summary>
    private void GoTo(int level) => Go(level);

    /// <summary>Goes back to a level, or stays at it, and forgets anything deeper.</summary>
    private void Go(int level, bool stay = false)
    {
        _dir = stay ? "in" : "out";
        ResetReplay();
        _level = level;
        if (level < 2) _task = null;
        if (level < 3) _turn = null;
        if (level >= 4) return;

        _request = null;
        _requestMode = null;
    }

    /// <summary>Shows all the conversations, or only those still to review.</summary>
    private void Filter(bool onlyToReview) => _onlyToReview = onlyToReview;

    /// <summary>Stops any replay and forgets what it had played.</summary>
    private void ResetReplay()
    {
        _replayStop?.Cancel();
        _replayStop = null;
        _replays.Clear();
        _replaying = false;
    }

    /// <summary>Replays the open turn in every mode at once, from the stored events at their own pace.</summary>
    private async Task ReplayAsync()
    {
        // Nothing is open to replay.
        if (_task is null || _turn is null) return;

        // Fetch the open turn's stored events for every mode that has it; a mode without any doesn't play.
        var plays = new List<(string Mode, string Question, IReadOnlyList<TurnEventRow> Events)>();
        foreach (var m in _tree.Modes)
        {
            if (m.Conversation(_task)?.Turns.FirstOrDefault(t => t.N == _turn) is not { } tu) continue;
            var events = await TurnReplays.FetchAsync(Api, _runId, m.Name, _repeat, _task, tu.N);
            if (events.Count > 0) plays.Add((m.Name, tu.Question, events));
        }

        if (plays.Count == 0) return;

        // Start every mode at once, each into its own turn, and wait until they all finish or someone stops them.
        var stop = _replayStop = new CancellationTokenSource();
        _replays.Clear();
        foreach (var (mode, question, _) in plays) _replays[mode] = new Turn { UserText = question };
        _replaying = true;
        StateHasChanged();
        try
        {
            await Task.WhenAll(plays.Select(p =>
                TurnReplays.PlayAsync(_replays[p.Mode], p.Events, StateHasChanged, stop.Token)));
        }
        catch (OperationCanceledException)
        {
        }

        _replaying = false;
        StateHasChanged();
    }

    /// <summary>Stops the replay.</summary>
    private void StopReplay()
    {
        _replayStop?.Cancel();
        _replaying = false;
    }

    /// <summary>Opens the chats side by side on a conversation, or the first one if none is given.</summary>
    private void OpenChats(string? task)
    {
        _chatTask = task;
        _chatOpen = true;
        _chatPlay++;
    }

    /// <summary>Closes the chats.</summary>
    private void CloseChats() => _chatOpen = false;
}
