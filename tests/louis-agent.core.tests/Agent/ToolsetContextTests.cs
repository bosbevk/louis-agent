using LouisAgent.Core.Agent;
using LouisAgent.Core.Config;
using LouisAgent.Core.Tools.Web.Browsing;

namespace LouisAgent.Core.Tests.Agent;

// F33 step 1: a toolset gets the guarded browser from its context, as it gets the search and the renderer: the one given
// (a test's fake), else the Chrome BROWSER_URL names, else none (a toolset that needs one is then unavailable).
public class ToolsetContextTests
{
    private sealed class NoSearch : LouisAgent.Core.Tools.Web.Search.IWebSearchProvider
    {
        public string Name => "none";

        public Task<IReadOnlyList<LouisAgent.Core.Tools.Web.Search.WebSearchResult>> SearchAsync(
            LouisAgent.Core.Tools.Web.Search.WebSearchQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<LouisAgent.Core.Tools.Web.Search.WebSearchResult>>([]);
    }

    private sealed class FakeBrowser : IWebBrowser
    {
        public Task<BrowserVisit> VisitAsync(Uri url, Func<Uri, bool>? capture = null, BrowserSession? session = null, string? script = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new BrowserVisit(url.ToString(), "", []));
    }

    [Test]
    public void TheBrowserGiven_IsTheOneUsed()
    {
        var fake = new FakeBrowser();
        Assert.That(new ToolsetContext(new AgentOptions(), new NoSearch(), WebBrowser: fake).Browser, Is.SameAs(fake));
    }

    [Test]
    public void BrowserUrl_GivesTheChromeItNames() =>
        Assert.That(new ToolsetContext(new AgentOptions { BrowserUrl = "http://puls-browser:9222" }, new NoSearch()).Browser,
            Is.TypeOf<ChromeWebBrowser>());

    [TestCase(null)]
    [TestCase("")]
    [TestCase("not a url")]
    public void WithoutAUsableBrowserUrl_ThereIsNoBrowser(string? url) =>
        Assert.That(new ToolsetContext(new AgentOptions { BrowserUrl = url }, new NoSearch()).Browser, Is.Null);
}
