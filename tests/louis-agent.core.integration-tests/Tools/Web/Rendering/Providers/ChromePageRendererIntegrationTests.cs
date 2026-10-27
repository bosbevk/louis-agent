using LouisAgent.Testing;
using LouisAgent.Core.Tools.Web.Browsing;
using LouisAgent.Core.Tools.Web.Rendering;
using LouisAgent.Core.Tools.Web.Rendering.Providers;

namespace LouisAgent.Core.IntegrationTests.Tools.Web.Rendering.Providers;

// F32 step 2, F33 step 1: the Chrome renderer reads a page's text through the guarded browser (ChromeWebBrowser, where
// the guards and their tests now live). What's the renderer's own: a page comes back as text, and the browser's errors
// come back as the renderer's (PageRenderException), so ReadRenderedPage's answers don't change.
[Category("Integration")]
public class ChromePageRendererIntegrationTests
{
    [Test, Category("Live")]
    public async Task APage_IsReadAsTheBrowserShowsIt()
    {
        string url = Outside.Stack("BROWSER_URL", "browser to render with");

        var page = await new ChromePageRenderer(new ChromeWebBrowser(new Uri(url!))).RenderAsync(new Uri("https://example.com/"));

        Assert.That(page.Text, Does.Contain("Example Domain"));
        Assert.That(page.Url, Does.StartWith("https://example.com"));
    }
}
