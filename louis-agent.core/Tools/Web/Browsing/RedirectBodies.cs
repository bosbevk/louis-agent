using PuppeteerSharp;

namespace LouisAgent.Core.Tools.Web.Browsing;

/// <summary>
/// Sees the failure a redirect's body carries, so the runtime doesn't report it (FX36). PuppeteerSharp (25.14, still
/// so at 25.16) sets every redirect response's body task to "Response body is unavailable for redirect responses" and
/// never awaits it, so when the garbage collector reached that task the runtime raised
/// <see cref="TaskScheduler.UnobservedTaskException"/> and the log said "a task failed and nothing awaited it", once
/// per redirect a page followed, long after the visit. Asking for the body awaits that task, which counts the failure
/// as seen; the failure itself is expected and dropped. Goes when a PuppeteerSharp release awaits its own task.
/// </summary>
/// <remarks>
/// A fault observer. Puppeteer's JavaScript keeps the error as a value in its <c>Deferred</c> and only makes a
/// rejecting promise when someone asks for the body, so nothing is left unhandled there; the C# port faults the
/// task up front, so the awaiting has to happen here. Beside the browser, not in the last-chance handler, so an
/// unawaited task anywhere else is still reported.
/// </remarks>
internal static class RedirectBodies
{
    /// <summary>A redirect is a response in the three hundreds; nothing else has this failure on its body.</summary>
    internal static bool IsRedirect(IResponse response) => (int)response.Status is >= 300 and < 400;

    /// <summary>Asks for the body the redirect doesn't have, and drops the failure that answers; never throws.</summary>
    internal static async Task ObserveAsync(IResponse response)
    {
        try
        {
            await response.TextAsync();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // The body a redirect doesn't have: awaited, so the failure is seen, and nothing to keep.
        }
    }
}
