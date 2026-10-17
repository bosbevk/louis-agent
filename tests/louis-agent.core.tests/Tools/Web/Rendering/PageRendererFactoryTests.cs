using LouisAgent.Core.Config;
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
