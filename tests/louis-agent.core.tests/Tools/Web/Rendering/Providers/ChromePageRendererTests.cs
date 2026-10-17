using LouisAgent.Core.Tools.Web.Rendering;
using LouisAgent.Core.Tools.Web.Rendering.Providers;

namespace LouisAgent.Core.Tests.Tools.Web.Rendering.Providers;

// F32 step 2: the headless Chrome renderer. Without a browser, a call says the browser didn't answer, never hangs. With
// one (BROWSER_URL set, as the Puls stack's puls-browser on 127.0.0.1:9222), the live tests render a real page and
// check that a redirect to an internal address is refused inside the browser; they skip, saying so, without it.
public class ChromePageRendererTests
{
    [Test]
    public void ABrowserThatDoesntAnswer_IsAPageRenderException_SayingSo()
    {
        var renderer = new ChromePageRenderer(new Uri("http://127.0.0.1:1"));

        Assert.That(renderer.Name, Is.EqualTo("Chrome"));
        Assert.That(() => renderer.RenderAsync(new Uri("https://example.com/")),
            Throws.TypeOf<PageRenderException>().With.Message.Contains("127.0.0.1:1"));
    }

    private static ChromePageRenderer Live()
    {
        string? url = Environment.GetEnvironmentVariable("BROWSER_URL");
        Assume.That(url, Is.Not.Null.And.Not.Empty, "BROWSER_URL isn't set: no browser to render with (the Puls stack's puls-browser).");
        return new ChromePageRenderer(new Uri(url!));
    }

    [Test, Category("Live")]
    public async Task APage_IsReadAsTheBrowserShowsIt()
    {
        var page = await Live().RenderAsync(new Uri("https://example.com/"));

        Assert.That(page.Text, Does.Contain("Example Domain"));
        Assert.That(page.Url, Does.StartWith("https://example.com"));
    }

    [Test, Category("Live")]
    public void ARedirectToAnInternalAddress_IsRefusedInsideTheBrowser()
    {
        var renderer = Live();
        var redirect = new Uri("https://httpbin.org/redirect-to?url=http%3A%2F%2F169.254.169.254%2Flatest%2Fmeta-data%2F");

        Assert.That(() => renderer.RenderAsync(redirect), Throws.TypeOf<PageRenderException>().With.Message.Contains("169.254.169.254"));
    }
}
