using LouisAgent.SessionStream.Pacing;
using LouisAgent.Web.Chats.Calls;
using LouisAgent.Web.Scripts;
using LouisAgent.Web.Text;
using LouisAgent.Web.Usage;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace LouisAgent.Web.Components.Widget.Bar.Caption;

/// <summary>
/// The agent's caption (F134.4): its last answer as a card with two sides. The front has its name, its words, the
/// choices it offered and a way into the box. The back says how the answer was made (F134.5).
/// </summary>
/// <remarks>
/// It draws what it is given and moves nothing but itself: the agent's own call to show a place is what lit the place
/// (F134's rule 2), and the caption's module only stands the caption beside it, on a wide screen.
/// <see cref="Captioned"/> decides when an answer stands as a caption. Its choices are pills, as the design drew
/// them, and not the talk's cards: a caption is small and stands over the page, so a choice is its label, with its
/// why as a tip. A press still asks with the label, as a card does. Both sides are always drawn, so the card can
/// turn, and the one turned away is inert.
/// </remarks>
public partial class AgentCaption : IAsyncDisposable
{
    /// <summary>Who is speaking: the agent's name.</summary>
    [Parameter, EditorRequired]
    public string Title { get; set; } = "";

    /// <summary>What it said, as markdown.</summary>
    [Parameter, EditorRequired]
    public string Words { get; set; } = "";

    /// <summary>What it asked over its choices, shown quietly; none when its words already are that question.</summary>
    [Parameter]
    public string? Question { get; set; }

    /// <summary>The choices it offered with them; none when it offered none.</summary>
    [Parameter]
    public Offered? Offer { get; set; }

    /// <summary>How the answer was made (F134.5), shown on the caption's back.</summary>
    [Parameter, EditorRequired]
    public AnswerWork Work { get; set; } = null!;

    /// <summary>The currency the answer's cost is shown in, as the talk's is.</summary>
    [Parameter]
    public CurrencyChoice? Currency { get; set; }

    /// <summary>Asks with a choice's label, as a card in the talk does.</summary>
    [Parameter]
    public EventCallback<string> OnChoose { get; set; }

    /// <summary>Asks to see the whole talk instead.</summary>
    [Parameter]
    public EventCallback OnTalk { get; set; }

    /// <summary>
    /// Asks to be put in the agent's box, to write an answer in the visitor's own words. The caption can't name the
    /// box by its id: a page may hold another chat's box with the same one.
    /// </summary>
    [Parameter]
    public EventCallback OnWrite { get; set; }

    /// <summary>
    /// Whether the answer showed a place on the page. Only then does the caption stand beside the place that is lit,
    /// since the mark of an earlier answer's place stays on the page.
    /// </summary>
    [Parameter]
    public bool Beside { get; set; }

    /// <summary>Whether the answer is still coming in: its choices wait until it is whole.</summary>
    [Parameter]
    public bool Streaming { get; set; }

    /// <summary>
    /// Whether the server already paced the answer's words (F156), so the caption shows them as they come and the words
    /// are paced once.
    /// </summary>
    [Parameter]
    public bool ServerPaced { get; set; }

    /// <summary>Asks to put the answer away. With nobody to ask, the caption shows no close.</summary>
    [Parameter]
    public EventCallback OnClose { get; set; }

    /// <summary>The chat's words.</summary>
    [Inject]
    private ChatText Text { get; set; } = null!;

    /// <summary>The browser, for the module that stands the caption beside the place.</summary>
    [Inject]
    private IJSRuntime JS { get; set; } = null!;

    /// <summary>The caption on the page.</summary>
    private ElementReference _caption;

    /// <summary>The caption's module, loaded once it is drawn.</summary>
    private JsModule? _module;

    /// <summary>Whether the caption is turned over to how the answer was made.</summary>
    private bool _turned;

    /// <summary>Whether the answer had shown a place when the module last stood the caption.</summary>
    private bool _stoodBeside;

    /// <summary>How often the caption steps its words on while they come, about thirty times a second.</summary>
    private static readonly TimeSpan Frame = TimeSpan.FromMilliseconds(33);

    /// <summary>
    /// Whether the words come at a pace (F154): only for an answer still streaming when the caption first draws, so an
    /// answer already done shows whole and a reload never types it out again.
    /// </summary>
    private bool _paced;

    /// <summary>How many letters show just now, part letters kept, as <see cref="WordPace"/> said.</summary>
    private double _shown;

    /// <summary>The module that grows the caption in and fades it out (F154).</summary>
    private JsModule? _motion;

    /// <summary>Stops the pace when the caption goes.</summary>
    private readonly CancellationTokenSource _pacing = new();

    /// <summary>The words to draw: those the pace has reached, or all of them once it has done.</summary>
    private string Shown => _paced ? Words[..Math.Min(Words.Length, WordPace.CountLetters(_shown))] : Words;

    /// <summary>Whether the answer is on its way out: the caption fades, and then says it has gone (F154).</summary>
    [Parameter]
    public bool Leaving { get; set; }

    /// <summary>Tells the bar the caption has faded, so it can be taken away.</summary>
    [Parameter]
    public EventCallback OnLeft { get; set; }

    /// <summary>Whether the caption has begun to fade, so it fades once.</summary>
    private bool _fading;

    /// <summary>
    /// An answer still streaming when the caption first draws comes at a pace, unless the server already paced it, so
    /// the words are paced once (F156).
    /// </summary>
    protected override void OnInitialized() => _paced = Streaming && !ServerPaced;

    /// <summary>
    /// Steps the words on about thirty times a second, as the pace says, until the answer is done and all have shown.
    /// </summary>
    private async Task PaceAsync(CancellationToken stop)
    {
        using var frames = new PeriodicTimer(Frame);
        long last = System.Diagnostics.Stopwatch.GetTimestamp();
        try
        {
            // Each tick waits free of the page's own turn, and the drawing is handed back to it.
            while (await frames.WaitForNextTickAsync(stop).ConfigureAwait(false))
            {
                TimeSpan since = System.Diagnostics.Stopwatch.GetElapsedTime(last);
                last = System.Diagnostics.Stopwatch.GetTimestamp();
                _shown = WordPace.Advance(_shown, Words.Length, since, done: !Streaming);
                if (!Streaming && WordPace.CountLetters(_shown) >= Words.Length) _paced = false;
                await InvokeAsync(StateHasChanged);
                if (!_paced) return;
            }
        }
        catch (OperationCanceledException)
        {
            // The caption went while its words were still coming.
        }
    }

    /// <summary>Turns the caption over, or back to the answer.</summary>
    private void Turn() => _turned = !_turned;

    /// <summary>The caption's classes: turned over, or facing with the answer.</summary>
    private string Classes => _turned ? "agent-caption is-turned" : "agent-caption";

    /// <summary>A choice's classes: a pill, marked when the agent suggests it or it is written in the box.</summary>
    private static string ClassesFor(OfferChoice choice) => choice switch
    {
        { Writes: not null } => "caption-choice is-written",
        { Suggested: true } => "caption-choice is-suggested",
        _ => "caption-choice"
    };

    /// <summary>The exact figures of what was measured, as the tip over the back's three.</summary>
    private string? Detail => Work.Usage is { } usage ? UsageFormat.Detail(usage, Currency) : null;

    /// <summary>
    /// Once on the page, the caption asks its module to stand it beside the place the agent lit. An answer's words
    /// can come before it shows its place, so when the place comes the caption is stood again.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _stoodBeside = Beside;
            _module = new JsModule(JS, FrameworkScripts.Caption);
            await _module.InvokeVoidAsync("standBeside", _caption);
            // It grows in from where it comes from (F154); a visitor who asked for less motion sees its words at once.
            _motion = new JsModule(JS, FrameworkScripts.CaptionMotion);
            try
            {
                if (await _motion.InvokeAsync<bool>("lessMotion")) _paced = false;
                await _motion.InvokeVoidAsync("growIn", _caption);
            }
            catch (JSException)
            {
                // No motion to be had: the words show whole rather than not at all.
                _paced = false;
                StateHasChanged();
            }

            if (_paced) _ = PaceAsync(_pacing.Token);
            return;
        }

        // On its way out, it fades once and then says so; a motion that fails only means it goes without fading.
        if (Leaving && !_fading)
        {
            _fading = true;
            try
            {
                if (_motion is not null) await _motion.InvokeVoidAsync("leave", _caption);
            }
            catch (JSException)
            {
            }

            await OnLeft.InvokeAsync();
            return;
        }

        if (_module is null || Beside == _stoodBeside) return;
        _stoodBeside = Beside;
        await _module.InvokeVoidAsync("standAgain", _caption);
    }

    /// <summary>The caption is going: its module stops following the page, and is let go.</summary>
    public async ValueTask DisposeAsync()
    {
        await _pacing.CancelAsync();
        _pacing.Dispose();
        GC.SuppressFinalize(this);
        if (_module is not null)
        {
            // First the page is let go of, the place lit and the dimming, whatever else fails after (Louis: "click
            // closed and stayed like this"). The browser may be gone already, with the page.
            try
            {
                await _module.InvokeVoidAsync("letGo", _caption);
            }
            catch (JSDisconnectedException)
            {
            }

            await _module.DisposeAsync();
        }

        if (_motion is null) return;
        // A motion module that never loaded fails here too, and the caption is going anyway.
        try
        {
            await _motion.DisposeAsync();
        }
        catch (JSException)
        {
        }
    }
}
