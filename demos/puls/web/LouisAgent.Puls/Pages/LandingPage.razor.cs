using LouisAgent.Web.Scripts;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace LouisAgent.Puls.Pages;

public partial class LandingPage : IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    /// <summary>The page's own module (CL31), beside the site's scripts: the landing's motion.</summary>
    private JsModule Landing => _landing ??= new JsModule(JS, "./js/landing.js");

    private JsModule? _landing;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender) await Landing.InvokeVoidAsync("start");
    }

    public async ValueTask DisposeAsync()
    {
        // The page's listeners and timers go with it; a closed browser tab has no JS runtime to tell.
        try
        {
            if (_landing is not null) await _landing.InvokeVoidAsync("stop");
        }
        catch (JSDisconnectedException)
        {
        }

        if (_landing is not null) await _landing.DisposeAsync();
    }
}
