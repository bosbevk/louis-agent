using LouisAgent.Web.Chats;
using LouisAgent.Web.Usage;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace LouisAgent.Web.Components;

public partial class TurnView
{
    [Parameter, EditorRequired] public Turn Turn { get; set; } = default!;
    [Parameter] public bool Streaming { get; set; }

    /// <summary>A site's actions under the user's message, such as copy and edit; none by default.</summary>
    [Parameter] public RenderFragment<Turn>? UserActions { get; set; }

    /// <summary>A site's actions under a finished answer, such as copy and regenerate; none by default.</summary>
    [Parameter] public RenderFragment<Turn>? AnswerActions { get; set; }

    /// <summary>The currency costs are shown in (F14); a change re-draws finished turns too.</summary>
    [Parameter] public CurrencyChoice? Currency { get; set; }

    private Turn? _renderedTurn;
    private bool _wasStreaming;
    private bool _shouldRender = true;
    private CurrencyChoice? _renderedCurrency;

    // Finished turns don't change, so only the streaming one (and its final render) is redrawn; markdown isn't
    // re-parsed for the whole conversation on every streamed word.
    protected override void OnParametersSet()
    {
        _shouldRender = Streaming || _wasStreaming || !ReferenceEquals(Turn, _renderedTurn) || Currency != _renderedCurrency;
        _renderedTurn = Turn;
        _renderedCurrency = Currency;
        _wasStreaming = Streaming;
    }

    protected override bool ShouldRender() => _shouldRender;

    /// <summary>
    /// The images the turn's tools gave, its specialists' included (FX22): drawn as images wherever the turn's text or
    /// a delegation card's answer shows them.
    /// </summary>
    private IReadOnlySet<string> Shown => ImageSources.Of(Turn);

    private ElementReference _answer;

    // Only runs after this turn actually re-rendered (see ShouldRender), so finished turns aren't rescanned.
    protected override async Task OnAfterRenderAsync(bool firstRender) =>
        await JS.InvokeVoidAsync("louisChat.highlightBlocks", _answer);
}
