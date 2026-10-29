using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Config;
using LouisAgent.Core.Tools.Web.Browsing;

namespace LouisAgent.Core.Tests.Agent.Toolsets;

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
        public int Calls { get; private set; }

        public Task<BrowserVisit> VisitAsync(Uri url, Func<Uri, bool>? capture = null, BrowserSession? session = null,
            string? script = null,
            string? readySelector = null, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(new BrowserVisit(url.ToString(), "", []));
        }
    }

    [Test]
    public async Task TheRendererAndTheShops_ReadThroughTheSameBrowser()
    {
        // F18 step 4a: one browser per process, so a recorded run holds every page, whichever tool read it; the renderer had built its own.
        var fake = new FakeBrowser();
        var context = new ToolsetContext(new AgentOptions { BrowserUrl = "http://puls-browser:9222" }, new NoSearch(),
            WebBrowser: fake);

        await context.Renderer.RenderAsync(new Uri("https://example.com/recipe"));

        Assert.That(context.Browser, Is.SameAs(fake));
        Assert.That(fake.Calls, Is.EqualTo(1), "the page the renderer read went through the browser the shops get");
    }

    [Test]
    public void TheBrowserGiven_IsTheOneUsed()
    {
        var fake = new FakeBrowser();
        Assert.That(new ToolsetContext(new AgentOptions(), new NoSearch(), WebBrowser: fake).Browser, Is.SameAs(fake));
    }

    [Test]
    public void TheChromeItNames_SaysWhatTheOptionsSay_ToEverySite()
    {
        // FX21: the user agent travels from the setting to the browser; unset, the browser keeps Chrome's own.
        var named = new ToolsetContext(
            new AgentOptions
            { BrowserUrl = "http://browser:9222", BrowserUserAgent = "Puls/1.0 (+https://example.test)" },
            new NoSearch());
        var plain = new ToolsetContext(new AgentOptions { BrowserUrl = "http://browser:9222" }, new NoSearch());

        Assert.That(((ChromeWebBrowser)named.Browser!).UserAgent, Is.EqualTo("Puls/1.0 (+https://example.test)"));
        Assert.That(((ChromeWebBrowser)plain.Browser!).UserAgent, Is.Null);
    }

    [Test]
    public void BrowserUrl_GivesTheChromeItNames() =>
        Assert.That(
            new ToolsetContext(new AgentOptions { BrowserUrl = "http://puls-browser:9222" }, new NoSearch()).Browser,
            Is.TypeOf<ChromeWebBrowser>());

    [TestCase(null)]
    [TestCase("")]
    [TestCase("not a url")]
    public void WithoutAUsableBrowserUrl_ThereIsNoBrowser(string? url) =>
        Assert.That(new ToolsetContext(new AgentOptions { BrowserUrl = url }, new NoSearch()).Browser, Is.Null);
}
