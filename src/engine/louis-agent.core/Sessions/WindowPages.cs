using LouisAgent.SessionStream.Wire.Requests;
using LouisAgent.SessionStream.Wire.Requests.Windows;

namespace LouisAgent.Core.Sessions;

/// <summary>
/// The pages of the agents' windows on the visitor's screen for the running turn (FX134): a showroom's live window
/// shows a business on a mock of its website, so an ask to that business sends it the page it sits in, as the
/// window's own message would.
/// </summary>
/// <remarks>
/// Ambient context, as <see cref="TurnDepth"/> is: the host sets it around the turn from the visitor's message, and
/// <c>AskAgent</c> reads it, so the model never carries a page. Outside a turn there are none.
/// </remarks>
public static class WindowPages
{
    private static readonly AsyncLocal<IReadOnlyList<WindowPageRequest>?> CurrentPages = new();

    /// <summary>The page of this agent's window, if one is on the screen.</summary>
    public static PageRequest? For(string identity) =>
        CurrentPages.Value?.FirstOrDefault(window =>
            string.Equals(window.Identity, identity, StringComparison.OrdinalIgnoreCase))?.Page;

    /// <summary>
    /// Runs what follows with these windows' pages, until the scope ends and the ones before come back.
    /// </summary>
    public static IDisposable Use(IReadOnlyList<WindowPageRequest>? pages)
    {
        var previous = CurrentPages.Value;
        CurrentPages.Value = pages;
        return new Restore(previous);
    }

    /// <summary>Puts the pages before back.</summary>
    private sealed class Restore(IReadOnlyList<WindowPageRequest>? previous) : IDisposable
    {
        /// <summary>The scope ends.</summary>
        public void Dispose() => CurrentPages.Value = previous;
    }
}
