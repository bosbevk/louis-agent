using Microsoft.JSInterop;

namespace LouisAgent.Web.Testing;

/// <summary>
/// A browser's JS runtime for a test without a renderer (ChatRunner's): every call is recorded by name and answered
/// with the type's default, as bUnit's loose interop does for a component.
/// </summary>
public sealed class RecordingJSRuntime : IJSRuntime
{
    public List<string> Calls { get; } = [];

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        Calls.Add(identifier);
        return ValueTask.FromResult(default(TValue)!);
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
        InvokeAsync<TValue>(identifier, args);
}
