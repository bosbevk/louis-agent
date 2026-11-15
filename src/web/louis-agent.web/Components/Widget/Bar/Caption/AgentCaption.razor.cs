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

    /// <summary>Whether the answer is still coming in: its choices wait until it is whole.</summary>
    [Parameter]
    public bool Streaming { get; set; }

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

    /// <summary>Turns the caption over, or back to the answer.</summary>
    private void Turn() => _turned = !_turned;

    /// <summary>The caption's classes: turned over, or facing with the answer.</summary>
    private string Classes => _turned ? "agent-caption is-turned" : "agent-caption";

    /// <summary>A choice's classes: a pill, marked when the agent suggests it or when it is written in the box.</summary>
    private static string ChoiceClasses(OfferChoice choice) => choice switch
    {
        { Writes: not null } => "caption-choice is-written",
        { Suggested: true } => "caption-choice is-suggested",
        _ => "caption-choice"
    };

    /// <summary>The exact figures of what was measured, as the tip over the back's three.</summary>
    private string? Detail => Work.Usage is { } usage ? UsageFormat.Detail(usage, Currency) : null;

    /// <summary>Once on the page, the caption asks its module to stand it beside the place the agent lit.</summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        _module = new JsModule(JS, FrameworkScripts.Caption);
        await _module.InvokeVoidAsync("standBeside", _caption);
    }

    /// <summary>The caption is going: its module stops following the page, and is let go.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_module is null) return;
        // The browser may be gone already, with the page.
        try
        {
            await _module.InvokeVoidAsync("letGo", _caption);
        }
        catch (JSDisconnectedException)
        {
        }

        await _module.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
