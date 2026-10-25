using LouisAgent.Core.Config;
using LouisAgent.Core.Tools.Web.Browsing;
using LouisAgent.Core.Tools.Web.Replay;
using LouisAgent.Core.Sessions.Replay.Store;

namespace LouisAgent.Core.Tests.Tools.Web.Browsing;

// F18 step 4a.2: the process's one browser, built from configuration: none without a usable BROWSER_URL; the Chrome it names with the
// user agent the setting says (FX21); and a recording browser around it when a store is passed (4b.1: the factory no longer reads REPLAY_DIRECTORY; the store's From does).
public class WebBrowserFactoryTests
{
    [TestCase(null)]
    [TestCase("")]
    [TestCase("not a url")]
    public void WithoutAUsableBrowserUrl_ThereIsNoBrowser_EvenWithAStore(string? url) =>
        Assert.That(WebBrowserFactory.Create(new AgentOptions { BrowserUrl = url }, new ReplayStore("/data/replay")), Is.Null);

    [Test]
    public void WithoutAStore_ItIsTheChromeItself_WithTheUserAgentTheSettingSays()
    {
        var browser = WebBrowserFactory.Create(new AgentOptions { BrowserUrl = "http://puls-browser:9222", BrowserUserAgent = "Puls/1.0 (+https://example.test)" });

        Assert.That(browser, Is.TypeOf<ChromeWebBrowser>());
        Assert.That(((ChromeWebBrowser)browser!).UserAgent, Is.EqualTo("Puls/1.0 (+https://example.test)"));
    }

    [Test]
    public void WithAStore_ItIsRecordedAroundTheSameChrome()
    {
        var browser = WebBrowserFactory.Create(new AgentOptions { BrowserUrl = "http://puls-browser:9222" }, new ReplayStore("/data/replay"));

        Assert.That(browser, Is.TypeOf<RecordedWebBrowser>());
    }

    [Test]
    public void AReplayDirectoryWithNoStorePassed_IsStillTheChromeItself() =>
        Assert.That(WebBrowserFactory.Create(new AgentOptions { BrowserUrl = "http://puls-browser:9222", ReplayDirectory = "/data/replay" }),
            Is.TypeOf<ChromeWebBrowser>(), "the factory no longer names the folder: the store does");
}
