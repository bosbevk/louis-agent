using LouisAgent.Core.Config;
using LouisAgent.Core.Tools.Web.Browsing;
using LouisAgent.Core.Tools.Web.Rendering;
using LouisAgent.Core.Tools.Web.Rendering.Providers;

namespace LouisAgent.Core.Tests.Tools.Web.Rendering;

// F32 step 2 (Louis: "ReadRenderedPage we need this for all pages from all providers"): the headless Chrome we run
// renders whatever the search found; Tavily's Extract stays selectable. PAGE_RENDERER is an enum (ADR 0024); unset, a
// browser we run comes first, then Tavily's key; a renderer that can't work says what to set on its first call.
public class PageRendererFactoryTests
{
    private const string Browser = "http://puls-browser:9222";

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

    [TestCase(null, Browser, "tvly-key", PageRendererKind.Chrome)]
    [TestCase(null, null, "tvly-key", PageRendererKind.Tavily)]
    [TestCase(null, null, null, null)]
    [TestCase("TAVILY", Browser, null, PageRendererKind.Tavily)]
    [TestCase("playwright", Browser, "tvly-key", null)]
    public void Resolve_NamesTheRendererThatCreateWouldPick(string? renderer, string? browserUrl, string? tavilyKey, PageRendererKind? expected)
    {
        // F18 step 7a.1: a run records which renderer the instance read pages with; null is none (unset and nothing configured, or a name we don't know).
        var options = new AgentOptions { PageRenderer = renderer, BrowserUrl = browserUrl, TavilyApiKey = tavilyKey };

        Assert.That(PageRendererFactory.Resolve(options), Is.EqualTo(expected));
    }

    /// <summary>
    /// F18 step 7a.1 follow-up: <c>IsReady</c> says whether the renderer the options select can work. Named and set up is ready; named without its setting is not; unset is the
    /// fallback's answer, and nothing set is not ready; an unknown name is not. And <c>Create</c> hands back the Null Object (the unavailable renderer) exactly when it is not ready.
    /// </summary>
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
        Assert.That(PageRendererFactory.Create(options) is UnavailablePageRenderer, Is.EqualTo(!expected));
    }

    [TestCase(null)]
    [TestCase("chrome")]
    public async Task TheBrowserGiven_IsTheOneTheChromeRendererReadsThrough(string? renderer)
    {
        // F18 step 4a: the process builds one browser and hands it in, so the renderer's reads are recorded with the shops'.
        var browser = new CountingBrowser();

        var made = PageRendererFactory.Create(new AgentOptions { PageRenderer = renderer, BrowserUrl = Browser }, browser: browser);
        await made.RenderAsync(new Uri("https://example.com/recipe"));

        Assert.That(made, Is.TypeOf<ChromePageRenderer>());
        Assert.That(browser.Calls, Is.EqualTo(1));
    }

    [Test]
    public void ABrowserGiven_NeverMakesChromeAvailableWhereBrowserUrlIsNotSet() =>
        Assert.That(PageRendererFactory.Create(new AgentOptions { PageRenderer = "chrome" }, browser: new CountingBrowser()),
            Is.TypeOf<UnavailablePageRenderer>());

    private sealed class CountingBrowser : IWebBrowser
    {
        public int Calls { get; private set; }

        public Task<BrowserVisit> VisitAsync(Uri url, Func<Uri, bool>? capture = null, BrowserSession? session = null, string? script = null,
            string? readySelector = null, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(new BrowserVisit(url.ToString(), "<html><body>hello</body></html>", []));
        }
    }

    [Test]
    public void ChromeWithoutABrowser_SaysToSetBrowserUrl() =>
        Assert.That(() => PageRendererFactory.Create(new AgentOptions { PageRenderer = "chrome" }).RenderAsync(new Uri("https://www.pnp.co.za/")),
            Throws.TypeOf<PageRenderException>().With.Message.Contains("BROWSER_URL"));

    [Test]
    public void AnUnknownRenderer_SaysWhichAreSupported()
    {
        Assert.That(() => PageRendererFactory.Create(new AgentOptions { PageRenderer = "playwright" }).RenderAsync(new Uri("https://www.pnp.co.za/")),
            Throws.TypeOf<PageRenderException>().With.Message.Contains("playwright").And.Message.Contains("chrome, tavily"));
        Assert.That(PageRendererFactory.SupportedRenderers, Is.EqualTo(new[] { "chrome", "tavily" }));
    }
}
