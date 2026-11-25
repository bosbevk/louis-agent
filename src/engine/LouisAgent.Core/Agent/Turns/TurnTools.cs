using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Agent.Turns;

/// <summary>
/// The tools the running turn's conversation sends, for code deep in the pipeline that has only a request's options:
/// the loop's final request puts these back, never the engine's live list, which another chat may have changed.
/// </summary>
/// <remarks>
/// Ambient Context, the shape <c>UsageScope</c> has: an <see cref="AsyncLocal{T}"/> the turn opens and closes, so the
/// guard reads the conversation's tools without every layer between passing them down. An async iterator gets its
/// caller's context back after every <c>yield</c>, so a streamed turn makes its scope current again before each step,
/// as it does its span.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0008-history-is-append-only.md">ADR 0008: history is append-only</seealso>
internal sealed class TurnTools : IDisposable
{
    /// <summary>The scope of the turn running on this flow, if any.</summary>
    private static readonly AsyncLocal<TurnTools?> CurrentScope = new();

    /// <summary>The scope that was current before this one opened, put back when it closes.</summary>
    private readonly TurnTools? _outer;

    /// <summary>A scope over the conversation's tools, inside whatever scope was current.</summary>
    private TurnTools(IReadOnlyList<AITool> tools, TurnTools? outer)
    {
        Tools = tools;
        _outer = outer;
    }

    /// <summary>The running turn's scope; none outside a turn.</summary>
    public static TurnTools? Current => CurrentScope.Value;

    /// <summary>The tools the turn's conversation sends.</summary>
    public IReadOnlyList<AITool> Tools { get; }

    /// <summary>Opens the turn's scope over its conversation's tools; dispose it when the turn ends.</summary>
    public static TurnTools Begin(IReadOnlyList<AITool> tools)
    {
        var scope = new TurnTools(tools, Current);
        CurrentScope.Value = scope;
        return scope;
    }

    /// <summary>
    /// Makes this scope current again, after a streamed turn's <c>yield</c> handed the caller's back.
    /// </summary>
    public void Resume() => CurrentScope.Value = this;

    /// <summary>Closes the scope, putting back the one that was current before.</summary>
    public void Dispose() => CurrentScope.Value = _outer;
}
