using LouisAgent.Testing;
using LouisAgent.Core.Tools.Web.Browsing;
using LouisAgent.Core.Tools.Web.Rendering;
using LouisAgent.Core.Tools.Web.Rendering.Providers;

namespace LouisAgent.Core.Tests.Tools.Web.Rendering.Providers;

// F32 step 2, F33 step 1: the Chrome renderer reads a page's text through the guarded browser (ChromeWebBrowser, where
// the guards and their tests now live). What's the renderer's own: a page comes back as text, and the browser's errors
// come back as the renderer's (PageRenderException), so ReadRenderedPage's answers don't change.
public class ChromePageRendererTests
{
    [Test]
    public void ABrowserThatDoesntAnswer_IsAPageRenderException_SayingSo()
    {
        var renderer = new ChromePageRenderer(new ChromeWebBrowser(new Uri("http://127.0.0.1:1")));

        Assert.That(renderer.Name, Is.EqualTo("Chrome"));
        Assert.That(() => renderer.RenderAsync(new Uri("https://example.com/")),
            Throws.TypeOf<PageRenderException>().With.Message.Contains("127.0.0.1:1"));
    }
}
