using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Puls.Admin.Explorer.Replay;
using LouisAgent.Puls.Admin.Explorer.Text;
using LouisAgent.Puls.Admin.Explorer.Tree;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Chats;
using LouisAgent.Web.Shared.Usage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace LouisAgent.Puls.Components.Admin.Explorer.Chats;

/// <summary>
/// Plays one conversation of every mode side by side, each turn after the other and the modes at once, from the
/// stored events; you can pick another conversation, play it again, or close it.
/// </summary>
/// <remarks>
/// The dialog owns its playing, and the explorer only says which conversation to start on and bumps
/// <see cref="Play"/> when a button asks for it again: parameters down, callbacks up, as the live run's panel does
/// (the unattended run's findings 8 and 12).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public partial class ChatsDialog : IDisposable
{
    private readonly Dictionary<string, List<Turn>> _chats = [];
    private int _played;
    private string? _task;
    private bool _playing;
    private CancellationTokenSource? _stop;

    [Inject] private AgentApi Api { get; set; } = null!;
    [Inject] private IJSRuntime Js { get; set; } = null!;

    /// <summary>The repeat's tree.</summary>
    [Parameter, EditorRequired]
    public RunTree Tree { get; set; } = RunTree.Empty;

    /// <summary>The run's id, for its events' address.</summary>
    [Parameter, EditorRequired]
    public string RunId { get; set; } = "";

    /// <summary>The repeat shown.</summary>
    [Parameter, EditorRequired]
    public int Repeat { get; set; }

    /// <summary>The conversation to start on; the first one when none is given.</summary>
    [Parameter]
    public string? StartTask { get; set; }

    /// <summary>Goes up each time a button asks for the chats, so the same conversation can play again.</summary>
    [Parameter]
    public int Play { get; set; }

    /// <summary>The currency to show money in.</summary>
    [Parameter]
    public CurrencyChoice? Currency { get; set; }

    /// <summary>Someone closed the dialog.</summary>
    [Parameter]
    public EventCallback OnClose { get; set; }

    /// <summary>Starts playing when the explorer asks for the chats, and only then.</summary>
    protected override async Task OnParametersSetAsync()
    {
        if (Play == _played) return;
        _played = Play;
        await StartAsync(StartTask);
    }

    /// <summary>Moves the focus into the dialog when it opens, so Escape closes it.</summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        // The page's script may not be there (a test): the dialog works without the focus.
        try
        {
            await Js.InvokeVoidAsync("pulsUi.focus", "#chats-dialog");
        }
        catch (JSException)
        {
        }
    }

    /// <summary>Stops the chats that are playing.</summary>
    public void Dispose() => _stop?.Cancel();

    /// <summary>
    /// Plays a conversation (the first one if none is given): fetches every turn's stored events for every mode,
    /// then plays all the modes at once.
    /// </summary>
    private async Task StartAsync(string? task)
    {
        // Starting again: stop whatever was playing.
        _stop?.Cancel();
        var stop = _stop = new CancellationTokenSource();
        _task = task ?? Tree.Tasks.FirstOrDefault();
        _chats.Clear();
        if (_task is null) return;

        // Every turn's stored events, per mode, fetched before anything plays, so the modes start together.
        var plays = new List<(string Mode, List<(string Question, IReadOnlyList<TurnEventRow> Events)> Turns)>();
        foreach (var m in Tree.Modes)
        {
            if (m.Conversation(_task) is not { } conversation) continue;
            var turns = new List<(string, IReadOnlyList<TurnEventRow>)>();
            foreach (var tu in conversation.Turns)
                turns.Add((tu.Question, await TurnReplays.FetchAsync(Api, RunId, m.Name, Repeat, _task, tu.N)));
            plays.Add((m.Name, turns));
        }

        // The chats were closed or started again while we were fetching, so this playing of them is over.
        if (stop.IsCancellationRequested) return;

        // Every mode starts empty and plays its turns one after the other, all the modes at once.
        foreach (var (mode, _) in plays) _chats[mode] = [];
        _playing = true;
        StateHasChanged();
        try
        {
            await Task.WhenAll(plays.Select(p => PlayModeAsync(p.Mode, p.Turns, stop.Token)));
        }
        catch (OperationCanceledException)
        {
        }

        if (stop.IsCancellationRequested) return;
        _playing = false;
        StateHasChanged();
    }

    /// <summary>Plays one mode's turns one after the other, each into a turn of its own.</summary>
    private async Task PlayModeAsync(string mode, List<(string Question, IReadOnlyList<TurnEventRow> Events)> turns,
        CancellationToken stop)
    {
        foreach (var (question, events) in turns)
        {
            var turn = new Turn { UserText = question };
            _chats[mode].Add(turn);
            if (events.Count > 0) await TurnReplays.PlayAsync(turn, events, StateHasChanged, stop);
            StateHasChanged();
        }
    }

    /// <summary>Stops the chats and tells the explorer to close them.</summary>
    private async Task CloseAsync()
    {
        _stop?.Cancel();
        await OnClose.InvokeAsync();
    }

    /// <summary>Closes the chats when you press Escape.</summary>
    private async Task KeyAsync(KeyboardEventArgs e)
    {
        if (e.Key == "Escape") await CloseAsync();
    }

    /// <summary>What a mode's column says of the conversation: its turns and cost, or that it hasn't it.</summary>
    private string Heading(ModeNode m) =>
        m.Conversation(_task) is { } c
            ? $"{c.Turns.Count} turns · {ExplorerText.Money(c.Cost, Currency)}"
            : "not in this mode";
}
