using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace LouisAgent.Puls.Pages;

public partial class LandingPage : IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender) await JS.InvokeVoidAsync("pulsLanding.start");
    }

    public async ValueTask DisposeAsync()
    {
        // The page's listeners and timers go with it; a closed browser tab has no JS runtime to tell.
        try { await JS.InvokeVoidAsync("pulsLanding.stop"); }
        catch (JSDisconnectedException) { }
    }
}
