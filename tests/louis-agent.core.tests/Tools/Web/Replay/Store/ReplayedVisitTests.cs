using LouisAgent.Core.Tools.Web.Browsing;
using LouisAgent.Core.Tools.Web.Replay.Store;

namespace LouisAgent.Core.Tests.Tools.Web.Replay.Store;

// F18 step 4a.2 (Louis's fold-in): a recording is a page or a failure, never both: the replay's answer is ToVisit(), which gives the
// visit or throws the same WebBrowserException the agent saw. Written first: both ToVisit()s threw NotImplementedException on the red run.
public class ReplayedVisitTests
{
    [Test]
    public void AnAnsweredRecording_GivesItsPageAsTheVisit()
    {
        var page = new ReplayPage("https://example.com/after", "<html>é</html>", ["{}"], "r", [new BrowserCookie("a", "b", ".example.com")]);

        BrowserVisit visit = new ReplayedVisit.Answered(page).ToVisit();

        Assert.That((visit.Url, visit.Html, visit.ScriptResult), Is.EqualTo(("https://example.com/after", "<html>é</html>", "r")));
        Assert.That(visit.Captured, Is.EqualTo(new[] { "{}" }));
        Assert.That(visit.Cookies, Is.EqualTo(new[] { new BrowserCookie("a", "b", ".example.com") }));
    }

    [Test]
    public void AFailedRecording_ThrowsTheRecordedTextAsAWebBrowserException()
    {
        var error = Assert.Throws<WebBrowserException>(() => new ReplayedVisit.Failed("The page at https://x refused it.").ToVisit())!;

        Assert.That(error.Message, Is.EqualTo("The page at https://x refused it."));
    }
}
