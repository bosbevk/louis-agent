using Microsoft.JSInterop;

namespace LouisAgent.Api.Site.Rendering;

/// <summary>
/// The script runtime a page gets on the server (F121): it answers every call with nothing. A page's scripts run in
/// the browser once the app starts, never in a prerender, which only gives a search engine the page's words.
/// </summary>
internal sealed class NoScripts : IJSRuntime
{
    /// <summary>The one instance; it keeps nothing.</summary>
    public static NoScripts Instance { get; } = new();

    /// <inheritdoc />
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
        ValueTask.FromResult(default(TValue)!);

    /// <inheritdoc />
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken,
        object?[]? args) => ValueTask.FromResult(default(TValue)!);
}
