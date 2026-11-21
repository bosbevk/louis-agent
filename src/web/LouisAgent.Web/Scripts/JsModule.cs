using Microsoft.JSInterop;

namespace LouisAgent.Web.Scripts;

/// <summary>
/// One JavaScript module as a component sees it (CL31): the component names the module's path once, calls its
/// functions, and lets it go when it's disposed. The module is imported the first time a function is called, so a
/// component that never needs the browser never loads it, and a site lists no script for it.
/// </summary>
/// <remarks>
/// Virtual Proxy (Gang of Four): the component talks to this as if the module were there, and the proxy does the
/// <c>import</c> dance on first use, once, however many calls race for it. Without it every component would carry
/// the same import, the same cached reference and the same disposal, and one would get it wrong.
/// </remarks>
/// <seealso href="https://learn.microsoft.com/aspnet/core/blazor/javascript-interoperability/javascript-isolation">JavaScript isolation in Blazor (Microsoft)</seealso>
public sealed class JsModule(IJSRuntime js, string path) : IAsyncDisposable
{
    private Task<IJSObjectReference>? _import;

    /// <summary>Where the module is, as the browser fetches it.</summary>
    public string Path => path;

    /// <summary>Calls a function the module exports and gives back what it returned.</summary>
    public async ValueTask<T> InvokeAsync<T>(string function, params object?[] args) =>
        await (await ModuleAsync()).InvokeAsync<T>(function, args);

    /// <summary>Calls a function the module exports for what it does, not what it returns.</summary>
    public async ValueTask InvokeVoidAsync(string function, params object?[] args) =>
        await (await ModuleAsync()).InvokeVoidAsync(function, args);

    /// <summary>Lets the module go, if it was ever loaded; a browser already gone is no error.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_import is null) return;
        var import = _import;
        _import = null;
        try
        {
            await (await import).DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
        }
    }

    /// <summary>The module, imported on the first call and shared by every call after, racing ones included.</summary>
    private Task<IJSObjectReference> ModuleAsync() =>
        _import ??= js.InvokeAsync<IJSObjectReference>("import", path).AsTask();
}
