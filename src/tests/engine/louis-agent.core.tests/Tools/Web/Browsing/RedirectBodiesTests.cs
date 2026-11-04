using System.Net;
using System.Runtime.CompilerServices;
using LouisAgent.Core.Tools.Web.Browsing;
using PuppeteerSharp;

namespace LouisAgent.Core.Tests.Tools.Web.Browsing;

// FX36: PuppeteerSharp marks every redirect's body as a failed task and never awaits it, so the runtime reported it
// when the garbage collector found it, and the browser's log said "a task failed and nothing awaited it" long after
// the visit. The browser now asks for the body, which awaits the failure and so counts it seen.
public class RedirectBodiesTests
{
    /// <summary>The failure PuppeteerSharp sets on a redirect's body, word for word.</summary>
    private static PuppeteerException RedirectFault() =>
        new("Response body is unavailable for redirect responses");

    /// <summary>A body task failed the way the library fails it: a completion source set to the exception.</summary>
    private static Task<string> FailedBody(Exception fault)
    {
        var body = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        body.TrySetException(fault);
        return body.Task;
    }

    [Test]
    public void ARedirectsBody_IsObserved_SoTheRuntimeDoesntReportIt()
    {
        var fault = RedirectFault();
        var reported = new List<Exception>();
        EventHandler<UnobservedTaskExceptionEventArgs> onLost = (_, e) =>
        {
            if (!e.Exception.InnerExceptions.Contains(fault)) return;
            reported.Add(fault);
            e.SetObserved();
        };
        TaskScheduler.UnobservedTaskException += onLost;

        try
        {
            ObserveAndDrop(fault);
            // The runtime reports a failed task nobody awaited only when it collects it.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.That(reported, Is.Empty, "the redirect's failed body task was never awaited");
        }
        finally
        {
            TaskScheduler.UnobservedTaskException -= onLost;
        }
    }

    /// <summary>
    /// The fix's whole life in one frame that ends before the collection, so nothing here keeps the task alive.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ObserveAndDrop(Exception fault)
    {
        var response = new FakeResponse(HttpStatusCode.Found, FailedBody(fault));
        RedirectBodies.ObserveAsync(response).GetAwaiter().GetResult();
    }

    [Test]
    public void ObservingARedirect_NeverThrows()
    {
        var response = new FakeResponse(HttpStatusCode.MovedPermanently, FailedBody(RedirectFault()));

        Assert.That(() => RedirectBodies.ObserveAsync(response), Throws.Nothing);
        Assert.That(response.BodyReads, Is.EqualTo(1));
    }

    [TestCase(HttpStatusCode.MovedPermanently, true)]
    [TestCase(HttpStatusCode.Found, true)]
    [TestCase(HttpStatusCode.TemporaryRedirect, true)]
    [TestCase(HttpStatusCode.PermanentRedirect, true)]
    [TestCase(HttpStatusCode.OK, false)]
    [TestCase(HttpStatusCode.NotFound, false)]
    public void ARedirect_IsAResponseInTheThreeHundreds(HttpStatusCode status, bool redirect)
    {
        // Only a redirect's body is read: a page's own responses stay unread, as before (a shop's catalogue is kept
        // only when the caller asks for it).
        var response = new FakeResponse(status, Task.FromResult("body"));

        Assert.That(RedirectBodies.IsRedirect(response), Is.EqualTo(redirect));
    }
}
