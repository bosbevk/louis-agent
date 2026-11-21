using PuppeteerSharp;

namespace LouisAgent.Core.Web.Browsing;

/// <summary>
/// Tells the one failure PuppeteerSharp loses by design (25.14, still so on its main branch): when a page or an iframe
/// goes before its script context exists, <c>IsolatedWorld.ClearContext</c> fails a task nothing waits on, and the
/// runtime reports it when the garbage collector gets there, in clumps, long after the page. Nothing is lost by it, and
/// it filled the error log. Goes when a release stops failing that task.
/// </summary>
/// <remarks>
/// A named exception filter, not a fault observer as <see cref="RedirectBodies"/> is: that task is private to the
/// library, so there's nothing here to await. It holds the type and the words exactly, so any other lost task is still
/// an Error, a timeout from the same library included.
/// </remarks>
internal static class LostContexts
{
    /// <summary>The library's words for it.</summary>
    private const string Failure = "Execution Context was destroyed";

    /// <summary>Whether this is that failure and no other.</summary>
    public static bool Are(Exception failure) => failure is PuppeteerException { Message: Failure };
}
