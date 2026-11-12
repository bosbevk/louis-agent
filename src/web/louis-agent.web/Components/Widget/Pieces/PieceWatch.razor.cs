using LouisAgent.Web.Chats;
using LouisAgent.Web.Scripts;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace LouisAgent.Web.Components.Widget.Pieces;

/// <summary>
/// Watches the page for a piece the visitor selects (F127.7, Louis: "each part on the page becomes a piece the agent
/// can elaborate on, and focus on. like select it"): a click on a piece, not on a link or a button inside it, rings the
/// piece and tells the widget which one, by its id and heading.
/// </summary>
/// <remarks>
/// A module beside its component (ADR 0093), holding the page's click listener for as long as the widget is on the
/// page; the widget decides what a pick does, through <see cref="Picked"/>, as a card's click goes up to its chat.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0093-a-script-is-a-module-beside-its-component.md">ADR 0093: a script is a module beside its component</seealso>
public partial class PieceWatch : IAsyncDisposable
{
    private JsModule? _module;
    private DotNetObjectReference<PieceWatch>? _self;

    [Inject] private IJSRuntime JS { get; set; } = null!;

    /// <summary>What a pick does: the widget asks the guide about the piece.</summary>
    [Parameter]
    public EventCallback<PageArea> Picked { get; set; }

    /// <summary>The module starts listening once the widget is on the page.</summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        _module = new JsModule(JS, FrameworkScripts.PieceWatch);
        _self = DotNetObjectReference.Create(this);
        await _module.InvokeVoidAsync("watch", _self);
    }

    /// <summary>The module's call: the visitor picked this piece.</summary>
    [JSInvokable]
    public Task Pick(string id, string heading)
    {
        var piece = new PageArea(id, heading);
        return Picked.InvokeAsync(piece);
    }

    /// <summary>What a piece the guide showed, or a card opened, does: the widget marks it selected, nothing sent.</summary>
    [Parameter]
    public EventCallback<PageArea> Shown { get; set; }

    /// <summary>The module's call: the guide showed this piece, or the visitor opened this card, so it is selected.</summary>
    [JSInvokable("Shown")]
    public Task ShownAsync(string id, string heading)
    {
        var piece = new PageArea(id, heading);
        return Shown.InvokeAsync(piece);
    }

    /// <summary>Takes the ring off the piece, when the chip is closed.</summary>
    public async Task ClearAsync()
    {
        if (_module is not null) await _module.InvokeVoidAsync("clear");
    }

    /// <summary>Stops listening and lets the module go.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            await _module.InvokeVoidAsync("unwatch");
            await _module.DisposeAsync();
        }

        _self?.Dispose();
    }
}
