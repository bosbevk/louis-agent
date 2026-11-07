using LouisAgent.Composition.Web.Browsing;
using LouisAgent.Composition.Web.Search;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Config;
using LouisAgent.Core.Sessions.Replay.Store;
using LouisAgent.Core.Web;
using LouisAgent.Core.Web.Browsing;
using LouisAgent.Core.Web.Replay;

namespace LouisAgent.Core.Web.Tests;

/// <summary>
/// The web as the library gives it to the engine (CL33, from F33 and F18): the browser BROWSER_URL names, with the
/// user agent the settings say, none without a usable address; the renderer and the browser read through the same
/// Chrome; a search provider of the host's own is recorded only where a store is kept.
/// </summary>
public class WebAccessTests
{
    private sealed class NoSearch : IWebSearchProvider
    {
        public string Name => "none";

        public Task<IReadOnlyList<WebSearchResult>> SearchAsync(WebSearchQuery query,
            CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<WebSearchResult>>([]);
    }

    private sealed class FakeBrowser : IWebBrowser
    {
        public Task<BrowserVisit> VisitAsync(Uri url, Func<Uri, bool>? capture = null, BrowserSession? session = null,
            string? script = null, string? readySelector = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new BrowserVisit(url.ToString(), "", []));
    }

    private static ToolsetContext Context(AgentOptions options) =>
        new(options, WebAccess.Create(options, null, new NoSearch()));

    /// <summary>The browser the engine gets is the Chrome BROWSER_URL names, behind the robots policy (ADR 0068).</summary>
    [Test]
    public void BrowserUrl_GivesTheChromeItNames_UnderTheRobotsPolicy() =>
        Assert.That(Context(new AgentOptions { BrowserUrl = "http://puls-browser:9222" }).Browser,
            Is.TypeOf<RobotsWebBrowser>());

    [TestCase(null)]
    [TestCase("")]
    [TestCase("not a url")]
    public void WithoutAUsableBrowserUrl_ThereIsNoBrowser(string? url) =>
        Assert.That(Context(new AgentOptions { BrowserUrl = url }).Browser, Is.Null);

    /// <summary>FX21: the user agent travels from the setting to the browser; unset, the browser keeps Chrome's own.</summary>
    [Test]
    public void TheChromeItNames_SaysWhatTheOptionsSay_ToEverySite()
    {
        var named = WebBrowserFactory.Create(new AgentOptions
        {
            BrowserUrl = "http://browser:9222",
            BrowserUserAgent = "Puls/1.0 (+https://example.test)"
        });
        var plain = WebBrowserFactory.Create(new AgentOptions { BrowserUrl = "http://browser:9222" });

        Assert.That(((ChromeWebBrowser)named!).UserAgent, Is.EqualTo("Puls/1.0 (+https://example.test)"));
        Assert.That(((ChromeWebBrowser)plain!).UserAgent, Is.Null);
    }

    /// <summary>A test's own browser, given as a part, is the one the tools get.</summary>
    [Test]
    public void TheBrowserGiven_IsTheOneUsed()
    {
        var fake = new FakeBrowser();

        var context = new ToolsetContext(new AgentOptions(), new WebParts(new NoSearch(), Browser: fake));

        Assert.That(context.Browser, Is.SameAs(fake));
    }

    /// <summary>F18 step 4b.2: a provider given by a host records like a real one, so a test can prove a replay with one.</summary>
    [Test]
    public void AHostsOwnSearchProvider_IsRecordedOnlyWhereAStoreIsKept()
    {
        var own = new NoSearch();
        string folder = Path.Combine(Path.GetTempPath(), "louis-replay-" + Guid.NewGuid().ToString("N"));

        var recorded = WebAccess.Create(new AgentOptions(), new ReplayStore(folder), own).Search;
        var plain = WebAccess.Create(new AgentOptions(), null, own).Search;

        Assert.That(recorded, Is.TypeOf<RecordedSearchProvider>());
        Assert.That(plain, Is.SameAs(own));
    }
}
