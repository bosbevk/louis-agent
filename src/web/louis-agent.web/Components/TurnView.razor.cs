using LouisAgent.Web.Chats;
using LouisAgent.Web.Scripts;
using LouisAgent.Web.Usage;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace LouisAgent.Web.Components;

/// <summary>One turn as the chat shows it: the message, the answer and their actions.</summary>
public partial class TurnView : IAsyncDisposable
{
    private JsModule? _code;

    /// <summary>The turn's own module, beside it (CL31): the code blocks' highlighting.</summary>
    private JsModule Code => _code ??= new JsModule(JS, FrameworkScripts.TurnView);

    [Parameter, EditorRequired] public Turn Turn { get; set; } = default!;
    [Parameter] public bool Streaming { get; set; }

    /// <summary>A site's actions under the user's message, such as copy and edit; none by default.</summary>
    [Parameter]
    public RenderFragment<Turn>? UserActions { get; set; }

    /// <summary>A site's actions under a finished answer, such as copy and regenerate; none by default.</summary>
    [Parameter]
    public RenderFragment<Turn>? AnswerActions { get; set; }

    /// <summary>The currency costs are shown in (F14); a change re-draws finished turns too.</summary>
    [Parameter]
    public CurrencyChoice? Currency { get; set; }

    /// <summary>
    /// Whether the answer shows what it thought and the tools it called; off, its words alone (F128.1).
    /// </summary>
    [Parameter]
    public bool ShowWork { get; set; } = true;

    /// <summary>
    /// Whether a finished answer shows what it used and cost; off on a business's website, where a customer never
    /// sees tokens (F127.2).
    /// </summary>
    [Parameter]
    public bool ShowUsage { get; set; } = true;

    /// <summary>The visitor's message after this turn, once there is one: what the cards it offered fold to.</summary>
    [Parameter]
    public string? Answer { get; set; }

    /// <summary>What a click on a card this turn offered does with its label.</summary>
    [Parameter]
    public EventCallback<string> Chosen { get; set; }

    /// <summary>Whether the visitor's message is a bubble; not when a card before it already shows it (FX142).</summary>
    [Parameter]
    public bool ShowUserText { get; set; } = true;

    private Turn? _renderedTurn;
    private string? _renderedAnswer;
    private bool _wasStreaming;
    private bool _shouldRender = true;
    private CurrencyChoice? _renderedCurrency;

    /// <summary>
    /// Finished turns don't change, so only the streaming one (and its final render) is redrawn; markdown isn't
    /// re-parsed for the whole conversation on every streamed word. A finished turn draws once more when the visitor
    /// answers it, so the cards it offered fold.
    /// </summary>
    protected override void OnParametersSet()
    {
        _shouldRender = Streaming || _wasStreaming || !ReferenceEquals(Turn, _renderedTurn) ||
                        Currency != _renderedCurrency || Answer != _renderedAnswer;
        _renderedTurn = Turn;
        _renderedAnswer = Answer;
        _renderedCurrency = Currency;
        _wasStreaming = Streaming;
    }

    /// <summary>Only a turn still streaming draws again; a finished one keeps what it drew.</summary>
    protected override bool ShouldRender() => _shouldRender;

    /// <summary>
    /// The images the turn's tools gave, its specialists' included (FX22): drawn as images wherever the turn's text or
    /// a delegation card's answer shows them.
    /// </summary>
    private IReadOnlySet<string> Shown => ImageSources.Of(Turn);

    private ElementReference _answer;

    /// <summary>Only runs after this turn actually re-rendered, so finished turns aren't rescanned.</summary>
    protected override async Task OnAfterRenderAsync(bool firstRender) =>
        await Code.InvokeVoidAsync("highlightBlocks", _answer);

    /// <summary>Lets the module go, if it was ever loaded.</summary>
    public ValueTask DisposeAsync() => _code?.DisposeAsync() ?? ValueTask.CompletedTask;
}
