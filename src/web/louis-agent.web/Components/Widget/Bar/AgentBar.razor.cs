using LouisAgent.Web.Chats;
using LouisAgent.Web.Components.Chat;
using LouisAgent.Web.Components.Widget.Bar.Caption;
using LouisAgent.Web.Components.Widget.Bar.Opening;
using LouisAgent.Web.Components.Widget.Bar.Sides;
using LouisAgent.Web.Components.Widget.Orb;
using LouisAgent.Web.Scripts;
using LouisAgent.Web.Text;
using LouisAgent.Web.Usage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace LouisAgent.Web.Components.Widget.Bar;

/// <summary>
/// The chat widget's bar (F134.3): the guide at the foot of the page. In it the orb, a line of what the guide is doing,
/// and the box, always there to type in. Over it the guide's last answer stands as a caption, and the whole talk
/// once the visitor opens it.
/// </summary>
/// <remarks>
/// A view of the widget's own runner, not a second chat: it reads the runner and hands a send or a choice back to the
/// widget, so the session, the door, the page that travels with a message and the tool calls stay in one place (F134's
/// rule 4, one engine with a layout). What it shows of the agent is read from the runner by
/// <see cref="OrbStates"/>, never guessed. It hears a place being shown itself, since only the bar shows it. It
/// stands fixed over the page, so its module tells the page how much room it takes (F134's rule 5).
/// </remarks>
public partial class AgentBar : IAsyncDisposable
{
    /// <summary>How long the orb shows a place after the guide's call to show it, since the call has no end.</summary>
    private static readonly TimeSpan ShowingFor = TimeSpan.FromSeconds(2.5);

    /// <summary>The widget's runner: the chat, the draft and whether it is streaming.</summary>
    [Parameter, EditorRequired]
    public ChatRunner Runner { get; set; } = null!;

    /// <summary>Who the guide is: the orb's name, and the line's words while it waits.</summary>
    [Parameter]
    public string Title { get; set; } = "";

    /// <summary>What the empty box says.</summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>The currency the talk's numbers are shown in.</summary>
    [Parameter]
    public CurrencyChoice? Currency { get; set; }

    /// <summary>Whether the talk shows the agent's work, as the widget's panel does.</summary>
    [Parameter]
    public bool ShowWork { get; set; } = true;

    /// <summary>Whether a card's answer shows in the talk, as the widget's panel does.</summary>
    [Parameter]
    public bool ShowCardAnswers { get; set; }

    /// <summary>Asks the widget to send what is in the box.</summary>
    [Parameter]
    public EventCallback OnSend { get; set; }

    /// <summary>Asks the widget to answer with a choice the guide offered.</summary>
    [Parameter]
    public EventCallback<string> OnChoose { get; set; }

    /// <summary>The chip's words for the piece the visitor selected (F134.5); none while nothing is selected.</summary>
    [Parameter]
    public string? Piece { get; set; }

    /// <summary>Asks the widget to let the selected piece go.</summary>
    [Parameter]
    public EventCallback OnClearPiece { get; set; }

    /// <summary>What the site stands on each side of the bar (FX168), when it wraps its widget in them.</summary>
    [CascadingParameter]
    private BarSides? Sides { get; set; }

    /// <summary>The chat's words.</summary>
    [Inject]
    private ChatText Text { get; set; } = null!;

    /// <summary>The site's services, where its guide's suggestions are when it names any.</summary>
    [Inject]
    private IServiceProvider Services { get; set; } = null!;

    /// <summary>The browser, for the module that tells the page the bar's room.</summary>
    [Inject]
    private IJSRuntime JS { get; set; } = null!;

    /// <summary>The bar on the page.</summary>
    private ElementReference _bar;

    /// <summary>The bar's own box, to put the visitor in it.</summary>
    private Composer? _box;

    /// <summary>The bar's module, loaded once it is drawn.</summary>
    private JsModule? _module;

    /// <summary>Whether the guide is talking, told to the page when the site keeps it (F134.4).</summary>
    private GuideActivity? Activity => Services.GetService<GuideActivity>();

    /// <summary>The questions the site's guide offers as chips; none when the site names none.</summary>
    private AgentSuggestions? Suggestions => Services.GetService<AgentSuggestions>();

    /// <summary>Whether the visitor opened the whole talk.</summary>
    private bool _talkOpen;

    /// <summary>How long the box stays open after the visitor leaves it, so a glance away does not close it.</summary>
    private static readonly TimeSpan ClosesAfter = TimeSpan.FromSeconds(3);

    /// <summary>Whether the visitor holds the bar open: they pressed the orb, or are in the box (FX172).</summary>
    private bool _held;

    /// <summary>Whether the visitor is in the box just now.</summary>
    private bool _inBox;

    /// <summary>Whether the pointer holds the bar open, on a site that opens it under the pointer (FX172).</summary>
    private bool _pointerOver;

    /// <summary>Whether the pointer has left the bar and not yet come back.</summary>
    private bool _pointerAway;

    /// <summary>When the guide last showed a place, while the orb still shows it.</summary>
    private DateTimeOffset? _shownAt;

    /// <summary>The runner this bar listens to, so a new one is heard and an old one let go.</summary>
    private ChatRunner? _heard;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (ReferenceEquals(_heard, Runner)) return;
        if (_heard is not null) _heard.ToolCalled -= OnToolCalled;
        if (_heard is not null) _heard.Changed -= OnChanged;
        _heard = Runner;
        _heard.ToolCalled += OnToolCalled;
        _heard.Changed += OnChanged;
    }

    /// <summary>Once on the page, the bar asks its module to keep the page told how much room it takes.</summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        _module = new JsModule(JS, FrameworkScripts.Bar);
        await _module.InvokeVoidAsync("keepRoom", _bar);
    }

    /// <summary>
    /// The runner moved on: the page is told whether the guide is answering. The runner tells from its stream and from
    /// its redraw timer, so the telling waits its turn on the bar's own dispatcher and reads the runner as it is then;
    /// a late timer's call can't leave the guide talking after the answer ended.
    /// </summary>
    private void OnChanged() => _ = InvokeAsync(() => Activity?.Set(Runner.Streaming));

    /// <summary>The guide called a tool: a call to show a place lights the orb's showing for a moment.</summary>
    private void OnToolCalled(Block block)
    {
        if (ShowPageCall.PathOf(block) is null) return;
        _shownAt = DateTimeOffset.UtcNow;
        _ = InvokeAsync(async () =>
        {
            StateHasChanged();
            await Task.Delay(ShowingFor);
            StateHasChanged();
        });
    }

    /// <summary>Whether a place was shown a moment ago.</summary>
    private bool Showing => _shownAt is { } at && DateTimeOffset.UtcNow - at < ShowingFor;

    /// <summary>The guide's last answer, done or still coming in; none before the first question.</summary>
    private Turn? Last => Runner.Chat?.Turns.LastOrDefault();

    /// <summary>What the orb shows, read from the runner.</summary>
    private OrbState State => OrbStates.Of(Facts);

    /// <summary>What the chat says of itself just now, for the orb's rule.</summary>
    private OrbFacts Facts => new(Runner.Streaming, Last?.EndsInWords == true, Showing, Heard);

    /// <summary>Whether the visitor is being heard: they are writing, or hold a piece of the page they picked.</summary>
    private bool Heard => !string.IsNullOrWhiteSpace(Runner.Draft) || Piece is not null;

    /// <summary>
    /// What the bar knows for whether its box is open (FX172): it starts closed, and opens while one side needs the
    /// other.
    /// </summary>
    private BarFacts Opening => new(_held || _pointerOver, Runner.Streaming, !string.IsNullOrWhiteSpace(Runner.Draft),
        Piece is not null, Caption is not null || TalkShows);

    /// <summary>Whether the site opens its bar under the pointer, as it named through its areas; a press if it named none.</summary>
    private bool OpensUnderThePointer => Services.GetService<BarOpening>()?.Opens == BarOpens.Hover;

    /// <summary>The pointer came onto the orb: on a site that opens under the pointer, that holds it open.</summary>
    private void PointerCame()
    {
        if (!OpensUnderThePointer) return;
        _pointerOver = true;
        _pointerAway = false;
    }

    /// <summary>The pointer left the bar: a moment later, if it has not come back, it no longer holds it open.</summary>
    private async Task PointerLeftAsync()
    {
        if (!_pointerOver) return;
        _pointerAway = true;
        await Task.Delay(ClosesAfter);
        if (!_pointerAway) return;
        _pointerOver = false;
        StateHasChanged();
    }

    /// <summary>The bar's classes: closed to the orb alone until one side needs the other.</summary>
    private string BarClasses => Opening.Open ? "agent-bar" : "agent-bar is-closed";

    /// <summary>Whether the orb's press has the box open, as a screen reader is told.</summary>
    private string Expanded => Opening.Open ? "true" : "false";

    /// <summary>
    /// The visitor pressed the orb: a closed box opens and they are put in it; an open one closes again (Louis: "when
    /// i click it again it must close"), its standing answer put away with it, unless the guide is still answering.
    /// </summary>
    private async Task HoldOpenAsync()
    {
        if (Opening.Open)
        {
            await CloseAsync();
            return;
        }

        _held = true;
        // Drawn open first, so the box is no longer out of reach when the visitor is put in it.
        StateHasChanged();
        await Task.Yield();
        await FocusTheBox();
    }

    /// <summary>
    /// Closes the box: the visitor's hold lets go, and an answer standing over it, or the whole talk, is put away. A
    /// guide still answering keeps it open, since the visitor would miss the words.
    /// </summary>
    private async Task CloseAsync()
    {
        _held = false;
        _pointerOver = false;
        _talkOpen = false;
        if (!Runner.Streaming && (Caption is not null || TalkShows)) await PutTheAnswerAway();
    }

    /// <summary>The visitor is in the box, which holds it open.</summary>
    private void Entered()
    {
        _held = true;
        _inBox = true;
    }

    /// <summary>
    /// The visitor left the box: a moment later, if they have not come back, it lets the bar close, unless something
    /// else still holds it open.
    /// </summary>
    private async Task LeftAsync()
    {
        _inBox = false;
        await Task.Delay(ClosesAfter);
        if (_inBox) return;
        _held = false;
        StateHasChanged();
    }

    /// <summary>The guide's mark on its orb: the first letter of its name, as the design drew it.</summary>
    private string? Initial => Title.Length > 0 ? Title[..1] : null;

    /// <summary>The line over the box: what the guide is doing, or its name while it waits.</summary>
    private string Doing => State switch
    {
        OrbState.Thinking => Text.Thinking,
        OrbState.Speaking => Text.AgentSpeaking,
        OrbState.Showing => Text.AgentShowing,
        _ => Title
    };

    /// <summary>Whether the last answer is the one the visitor put away.</summary>
    private bool IsPutAway => Last is { PutAway: true };

    /// <summary>
    /// The last answer as a caption (F134.4), from its first words on, until the visitor puts it away or opens the
    /// whole talk in its place.
    /// </summary>
    private Captioned? Caption => _talkOpen || IsPutAway ? null : Captioned.Of(Last);

    /// <summary>
    /// The whole talk shows once the visitor opens it. It opens by itself for an answer a caption can't hold, such as
    /// a form to fill in, so the visitor never has to look for what the guide asked.
    /// </summary>
    private bool TalkShows => _talkOpen || (!IsPutAway && Captioned.NeedsTheTalk(Last));

    /// <summary>
    /// The chips show while the guide waits with nothing over the bar, listening or not. They give way to a caption, which holds the
    /// guide's own choices and the way into the box, and to the whole talk (F134.4).
    /// </summary>
    private bool ChipsShow => State is OrbState.Idle or OrbState.Listening && Caption is null && !TalkShows;

    /// <summary>
    /// The chips to draw: the site's own questions before any talk, and after it a next step from where the talk
    /// was, the choices the guide last offered.
    /// </summary>
    private AgentChips Chips => AgentChips.Of(Suggestions, Captioned.Of(Last)?.Offer);

    /// <summary>What the talk has cost so far (F134.5), the chat's own sum; none while nothing is measured.</summary>
    private TurnUsage? Total => Runner.Chat?.Usage();

    /// <summary>How many answers the talk holds.</summary>
    private int Answers => Runner.Chat?.Turns.Count ?? 0;

    /// <summary>Opens the whole talk, or folds it away again.</summary>
    private async Task ToggleTalk()
    {
        bool shows = TalkShows;
        _talkOpen = !shows;
        // A talk that opened by itself, for a form, would open again at once: folding it puts that answer away.
        if (shows && Last is { } last && Captioned.NeedsTheTalk(last)) await Runner.PutAwayAsync(last);
    }

    /// <summary>
    /// Puts the last answer away: its caption goes, the turn says so in the saved chat so a reload keeps it away
    /// (FX161), and the next answer stands again.
    /// </summary>
    private async Task PutTheAnswerAway()
    {
        if (Last is { } last) await Runner.PutAwayAsync(last);
        // The page goes back as it was: the piece picked is let go with the answer (Louis: "when I close the answer
        // it resets").
        await OnClearPiece.InvokeAsync();
    }

    /// <summary>
    /// Puts the visitor in the bar's own box. The bar holds its box and asks it, since a page may hold another chat's
    /// box with the same id, and a label that names the id would reach that one.
    /// </summary>
    private async Task FocusTheBox()
    {
        if (_box is not null) await _box.FocusAsync();
    }

    /// <summary>The bar is going: it stops hearing its runner, and the page gets its room back.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_heard is not null) _heard.ToolCalled -= OnToolCalled;
        if (_heard is not null) _heard.Changed -= OnChanged;
        GC.SuppressFinalize(this);
        if (_module is null) return;

        // The browser may be gone already, with the page.
        try
        {
            await _module.InvokeVoidAsync("letGo", _bar);
        }
        catch (JSDisconnectedException)
        {
        }

        await _module.DisposeAsync();
    }
}
