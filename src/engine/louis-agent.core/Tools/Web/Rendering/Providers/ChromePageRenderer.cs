namespace LouisAgent.Core.Tools.Web.Rendering.Providers;

using LouisAgent.Core.Tools.Web.Fetching;
using LouisAgent.Core.Tools.Web.Browsing;
using LouisAgent.Core.Tools.Web.Rendering;

/// <summary>
/// A page rendered by the headless Chrome we run (F32 step 2; Louis: "ReadRenderedPage we need this for all pages from
/// all providers"), read through the guarded browser (<see cref="IWebBrowser"/>, F33 step 1: where the address rule, the
/// WebSocket block, cancellation and the page limit live). The page's HTML is read the way <see cref="WebTools.FetchUrl"/>
/// reads HTML; the browser's errors come back as the renderer's own, so ReadRenderedPage's answers don't change.
/// </summary>
public sealed class ChromePageRenderer(IWebBrowser browser) : IPageRenderer
{
    public string Name => "Chrome";

    public async Task<RenderedPage> RenderAsync(Uri url, CancellationToken cancellationToken = default)
    {
        BrowserVisit visit;
        try
        {
            visit = await browser.VisitAsync(url, cancellationToken: cancellationToken);
        }
        catch (WebBrowserException ex)
        {
            throw new PageRenderException(ex.Message);
        }

        var (title, text) = HtmlText.ToText(visit.Html, new Uri(visit.Url));
        return new RenderedPage(visit.Url, string.IsNullOrWhiteSpace(title) ? text : $"# {title}\n\n{text}");
    }
}
