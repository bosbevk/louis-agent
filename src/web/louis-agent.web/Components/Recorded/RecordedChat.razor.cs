using System.Net.Http;
using LouisAgent.SessionStream;
using LouisAgent.Web.Chats;
using LouisAgent.Web.Chats.Replay;
using LouisAgent.Web.Usage;
using Microsoft.AspNetCore.Components;
using ChatModel = LouisAgent.Web.Chats.Chat;

namespace LouisAgent.Web.Components.Recorded;

/// <summary>
/// Plays a recorded session in the chat's own view (F120): a site hands over its turns, each a question and where its
/// recording is (F128.2: a conversation of several), and a visitor watches the agent think, call its tools and answer,
/// as it did, without a model running. When it's over the site's ending shows (an offer, a link), with a button to
/// play it again.
/// </summary>
/// <remarks>
/// A recording is the stream the API sent, saved as a file, so this reads it with the same reader and folds it with the
/// same <see cref="TurnStream"/> as a live chat: what a visitor sees is exactly what the agent's own chat drew. The
/// waits come from <see cref="RecordingPace"/> and the loop from <see cref="TurnReplay"/>, both plain classes with
/// their own tests; this component only fetches, holds the chat and draws.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md">ADR 0084: every site is built on the framework's web</seealso>
public partial class RecordedChat : IDisposable
{
    private ChatModel _chat = new() { SessionId = "recorded" };
    private readonly Dictionary<string, List<SessionEvent>> _recordings = [];
    private CancellationTokenSource? _stop;
    private bool _playing;
    private bool _ended;

    [Inject] private HttpClient Http { get; set; } = null!;

    /// <summary>The conversation's turns, in order: each question and its recording.</summary>
    [Parameter]
    public IReadOnlyList<RecordedTurn> Turns { get; set; } = [];

    /// <summary>Whether the agent's work shows (its thinking and tool calls); off, a customer's view.</summary>
    [Parameter]
    public bool ShowWork { get; set; } = true;

    /// <summary>The window's title line.</summary>
    [Parameter]
    public string Title { get; set; } = "";

    /// <summary>What the play-again button says.</summary>
    [Parameter]
    public string PlayAgain { get; set; } = "";

    /// <summary>How fast it plays: 1 as recorded, 0 for no waits at all.</summary>
    [Parameter]
    public double Pace { get; set; } = 1;

    /// <summary>The currency the bill shows in, as the live chat's does; none shows the ledger's own dollars.</summary>
    [Parameter]
    public CurrencyChoice? Currency { get; set; }

    /// <summary>What shows when the play is over: the site's offer.</summary>
    [Parameter]
    public RenderFragment? Ending { get; set; }

    /// <summary>Starts playing once the window is on the page.</summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender) await PlayAsync();
    }

    /// <summary>
    /// Plays the conversation from the start, into a fresh chat, one turn after another; a play already going stops
    /// first.
    /// </summary>
    private async Task PlayAsync()
    {
        _stop?.Cancel();
        _stop = new CancellationTokenSource();
        var stop = _stop.Token;
        _chat = new ChatModel { SessionId = "recorded" };
        (_playing, _ended) = (true, false);
        StateHasChanged();

        // A page that goes away, or a second play, stops this one where it is.
        try
        {
            foreach (var recorded in Turns)
                await PlayTurnAsync(recorded, stop);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        (_playing, _ended) = (false, true);
        StateHasChanged();
    }

    /// <summary>Adds one turn's question to the chat and plays its recording into it, read once and kept.</summary>
    private async Task PlayTurnAsync(RecordedTurn recorded, CancellationToken stop)
    {
        if (!_recordings.TryGetValue(recorded.Source, out var events))
            _recordings[recorded.Source] = events = await ReadAsync(recorded.Source, stop);

        var turn = new Turn { UserText = recorded.Question };
        _chat.Turns.Add(turn);
        StateHasChanged();
        await TurnReplay.PlayAsync(turn, RecordingPace.Steps(events, Pace), () => _ = InvokeAsync(StateHasChanged),
            stop);
    }

    /// <summary>
    /// A recording's events; none when the site can't serve it, so its turn is the question alone.
    /// </summary>
    private async Task<List<SessionEvent>> ReadAsync(string source, CancellationToken stop)
    {
        var events = new List<SessionEvent>();
        try
        {
            using var response = await Http.GetAsync(source, HttpCompletionOption.ResponseHeadersRead, stop);
            if (!response.IsSuccessStatusCode) return events;

            await using var stream = await response.Content.ReadAsStreamAsync(stop);
            await foreach (var streamEvent in SessionStreamReader.ReadAsync(stream, stop)) events.Add(streamEvent);
        }
        // A recording that can't be fetched plays nothing.
        catch (HttpRequestException)
        {
        }

        return events;
    }

    /// <summary>Stops the play when the page goes away.</summary>
    public void Dispose()
    {
        _stop?.Cancel();
        _stop?.Dispose();
        GC.SuppressFinalize(this);
    }
}
