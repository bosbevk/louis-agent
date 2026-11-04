using LouisAgent.Composition.Messages;
using LouisAgent.Composition.Web.Browsing;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Tests.Tools.Web.Replay;
using LouisAgent.Core.Tools.Web.Browsing;
using LouisAgent.Core.Tools.Web.Messages;
using LouisAgent.Core.Tools.Web.Robots;

namespace LouisAgent.Core.Tests.Tools.Web.Browsing;

/// <summary>
/// The browser that asks robots.txt before it opens a page, and the page's own meta tag after. Written first:
/// <c>RobotsWebBrowser.VisitAsync</c> threw NotImplementedException on the red run.
/// </summary>
public class RobotsWebBrowserTests
{
    private static readonly Uri Allowed = new("https://a.com/recipes/bobotie/");

    /// <summary>A policy over a fake robots.txt for every site.</summary>
    private static RobotsPolicy PolicyFor(RobotsFile file) =>
        new((_, _) => Task.FromResult(file), TimeProvider.System);

    /// <summary>A file that closes /private/ and nothing else.</summary>
    private static RobotsFile ClosesPrivate => new(200, "User-agent: *\nDisallow: /private/\n");

    /// <summary>A disallowed page is refused with the reason, and Chrome is never opened.</summary>
    [Test]
    public void ADisallowedPage_IsRefused_AndTheInnerBrowserIsNeverCalled()
    {
        var inner = new FakeBrowser();
        var browser = new RobotsWebBrowser(inner, PolicyFor(ClosesPrivate));

        var failure =
            Assert.ThrowsAsync<WebBrowserException>(() => browser.VisitAsync(new Uri("https://a.com/private/x")));

        Assert.That(failure.Message, Is.EqualTo(MessageText.Format(WebToolMessage.RobotsDisallows,
            new
            {
                Host = "a.com",
                Path = "/private/x"
            })));
        Assert.That(inner.Calls, Is.Zero);
    }

    /// <summary>
    /// A site whose robots.txt couldn't be read is refused with that reason, and Chrome is not opened.
    /// </summary>
    [Test]
    public void AnUnreachableFile_IsRefused_AndTheInnerBrowserIsNeverCalled()
    {
        var inner = new FakeBrowser();
        var browser = new RobotsWebBrowser(inner, PolicyFor(new RobotsFile(503, null)));

        var failure = Assert.ThrowsAsync<WebBrowserException>(() => browser.VisitAsync(Allowed));

        Assert.That(failure.Message, Is.EqualTo(MessageText.Format(WebToolMessage.RobotsUnreachable,
            new
            {
                Host = "a.com",
                Reason = "503"
            })));
        Assert.That(inner.Calls, Is.Zero);
    }

    /// <summary>An allowed page comes back exactly as the inner browser left it.</summary>
    [Test]
    public async Task AnAllowedPage_PassesThroughUnchanged()
    {
        var visit = new BrowserVisit(Allowed.AbsoluteUri, "<html><body>bobotie</body></html>", ["{}"]);
        var browser = new RobotsWebBrowser(new FakeBrowser(visit), PolicyFor(ClosesPrivate));

        var result = await browser.VisitAsync(Allowed);

        Assert.That(result, Is.SameAs(visit));
    }

    /// <summary>
    /// A page that carries the noai meta is read by Chrome and then refused, before the model sees it.
    /// </summary>
    [Test]
    public void APageWithTheNoAiMeta_IsRefusedAfterTheVisit()
    {
        var inner = new FakeBrowser(new BrowserVisit(Allowed.AbsoluteUri,
            "<html><head><meta name=\"robots\" content=\"noai, noimageai\"></head></html>", []));
        var browser = new RobotsWebBrowser(inner, PolicyFor(ClosesPrivate));

        var failure = Assert.ThrowsAsync<WebBrowserException>(() => browser.VisitAsync(Allowed));

        Assert.That(failure.Message, Is.EqualTo(MessageText.Format(WebToolMessage.PageRefusesAiReaders,
            new
            {
                Host = "a.com",
                Signal = "meta robots noai"
            })));
        Assert.That(inner.Calls, Is.EqualTo(1));
    }
}
