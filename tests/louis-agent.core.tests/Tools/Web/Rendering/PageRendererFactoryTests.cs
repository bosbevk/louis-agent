using LouisAgent.Core.Config;
using LouisAgent.Core.Tools.Web.Browsing;
using LouisAgent.Core.Tools.Web.Rendering;
using LouisAgent.Core.Tools.Web.Rendering.Providers;

namespace LouisAgent.Core.Tests.Tools.Web.Rendering;

/// <summary>Which page renderer the settings pick: the one named, or else our own Chrome if we know where it runs, or else Tavily's. One that can't work tells the model what to set the first time it's used.</summary>
public class PageRendererFactoryTests
{
    /// <summary>The address of the browser these tests pretend to have.</summary>
    private const string Browser = "http://puls-browser:9222";

    /// <summary>The renderer follows the settings: the one named, otherwise Chrome if we know where it runs, otherwise Tavily. One that can't work is the "not set up" renderer.</summary>
    [TestCase(null, Browser, null, typeof(ChromePageRenderer))]
    [TestCase(null, Browser, "tvly-key", typeof(ChromePageRenderer))]
    [TestCase(null, null, "tvly-key", typeof(TavilyPageRenderer))]
    [TestCase(null, null, null, typeof(UnavailablePageRenderer))]
    [TestCase("chrome", Browser, "tvly-key", typeof(ChromePageRenderer))]
    [TestCase("Chrome", null, "tvly-key", typeof(UnavailablePageRenderer))]
    [TestCase("tavily", Browser, "tvly-key", typeof(TavilyPageRenderer))]
    [TestCase("TAVILY", Browser, null, typeof(UnavailablePageRenderer))]
    [TestCase("playwright", Browser, "tvly-key", typeof(UnavailablePageRenderer))]
    public void TheRenderer_IsPickedFromConfiguration(string? renderer, string? browserUrl, string? tavilyKey, Type expected)
    {
        var options = new AgentOptions { PageRenderer = renderer, BrowserUrl = browserUrl, TavilyApiKey = tavilyKey };

        Assert.That(PageRendererFactory.Create(options), Is.TypeOf(expected));
    }

    /// <summary>Names the renderer `Create` would build, or nothing for an unknown name or when none is set up.</summary>
    [TestCase(null, Browser, "tvly-key", PageRendererKind.Chrome)]
    [TestCase(null, null, "tvly-key", PageRendererKind.Tavily)]
    [TestCase(null, null, null, null)]
    [TestCase("TAVILY", Browser, null, PageRendererKind.Tavily)]
    [TestCase("playwright", Browser, "tvly-key", null)]
    public void Resolve_NamesTheRendererThatCreateWouldPick(string? renderer, string? browserUrl, string? tavilyKey, PageRendererKind? expected)
    {
        // Nothing comes back for a name we don't know, or when nothing is set up.
        var options = new AgentOptions { PageRenderer = renderer, BrowserUrl = browserUrl, TavilyApiKey = tavilyKey };

        Assert.That(PageRendererFactory.Resolve(options), Is.EqualTo(expected));
    }

    /// <summary>Says whether the renderer you'd get is set up, and `Create` agrees: you get the "not set up" renderer exactly when this says no.</summary>
    [TestCase("chrome", Browser, null, true)]
    [TestCase("chrome", null, "tvly-key", false)]
    [TestCase("tavily", null, "tvly-key", true)]
    [TestCase("tavily", Browser, null, false)]
    [TestCase(null, Browser, null, true)]
    [TestCase(null, null, "tvly-key", true)]
    [TestCase(null, null, null, false)]
    [TestCase("playwright", Browser, "tvly-key", false)]
    public void IsReady_SaysWhetherTheSelectedRendererCanWork_AndCreateAgrees(string? renderer, string? browserUrl, string? tavilyKey, bool expected)
    {
        var options = new AgentOptions { PageRenderer = renderer, BrowserUrl = browserUrl, TavilyApiKey = tavilyKey };

        Assert.That(PageRendererFactory.IsReady(options), Is.EqualTo(expected));

        // Create agrees: the "not set up" renderer comes back exactly when it isn't ready.
        Assert.That(PageRendererFactory.Create(options) is UnavailablePageRenderer, Is.EqualTo(!expected));
    }

    /// <summary>Chrome reads through the browser it is handed, so a recorded run holds every page.</summary>
    [TestCase(null)]
    [TestCase("chrome")]
    public async Task TheBrowserGiven_IsTheOneTheChromeRendererReadsThrough(string? renderer)
    {
        // The process builds one browser and hands it in, so Chrome's reads are recorded along with the shops'.
        var browser = new CountingBrowser();

        var made = PageRendererFactory.Create(new AgentOptions { PageRenderer = renderer, BrowserUrl = Browser }, browser: browser);
        await made.RenderAsync(new Uri("https://example.com/recipe"));

        Assert.That(made, Is.TypeOf<ChromePageRenderer>());
        Assert.That(browser.Calls, Is.EqualTo(1));
    }

    /// <summary>Handing over a browser doesn't make Chrome available when we don't know where Chrome runs.</summary>
    [Test]
    public void ABrowserGiven_NeverMakesChromeAvailableWhereBrowserUrlIsNotSet() =>
        Assert.That(PageRendererFactory.Create(new AgentOptions { PageRenderer = "chrome" }, browser: new CountingBrowser()),
            Is.TypeOf<UnavailablePageRenderer>());

    /// <summary>A fake browser that counts how often it is asked to visit a page.</summary>
    private sealed class CountingBrowser : IWebBrowser
    {
        /// <summary>How many pages it has been asked to visit.</summary>
        public int Calls { get; private set; }

        /// <summary>Pretends to visit the page, and counts the visit.</summary>
        public Task<BrowserVisit> VisitAsync(Uri url, Func<Uri, bool>? capture = null, BrowserSession? session = null, string? script = null,
            string? readySelector = null, CancellationToken cancellationToken = default)
        {
            Calls++;

            // A page that renders to a little HTML, so the renderer has something to read.
            return Task.FromResult(new BrowserVisit(url.ToString(), "<html><body>hello</body></html>", []));
        }
    }

    /// <summary>Chrome with no browser address gives a renderer that says to set `BROWSER_URL`.</summary>
    [Test]
    public void ChromeWithoutABrowser_SaysToSetBrowserUrl() =>
        Assert.That(() => PageRendererFactory.Create(new AgentOptions { PageRenderer = "chrome" }).RenderAsync(new Uri("https://www.pnp.co.za/")),
            Throws.TypeOf<PageRenderException>().With.Message.Contains("BROWSER_URL"));

    /// <summary>A renderer name we do not know gives one that lists the names we do.</summary>
    [Test]
    public void AnUnknownRenderer_SaysWhichAreSupported()
    {
        // The error names what we were given and the names we do support.
        Assert.That(() => PageRendererFactory.Create(new AgentOptions { PageRenderer = "playwright" }).RenderAsync(new Uri("https://www.pnp.co.za/")),
            Throws.TypeOf<PageRenderException>().With.Message.Contains("playwright").And.Message.Contains("chrome, tavily"));

        // And that list of names is the enum's, in order.
        Assert.That(PageRendererFactory.SupportedRenderers, Is.EqualTo(new[] { "chrome", "tavily" }));
    }
}
