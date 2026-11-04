using LouisAgent.Core.Tools.Web.Robots;

namespace LouisAgent.Core.Tools.Web.Browsing;

/// <summary>
/// Asks the site's robots.txt before the browser opens a page, and refuses it if the site says no, so Chrome is never
/// even started for it. After the visit it reads the page's own meta tag and refuses a page that says no to AI readers.
/// A <see cref="BrowserVisit"/> has no response headers, so the page's headers are not looked at here, only its HTML;
/// the fetcher sees both.
/// </summary>
/// <remarks>
/// Decorator (GoF), as <c>RecordedWebBrowser</c> is: the same <see cref="IWebBrowser"/> around another one, adding
/// a check without changing what the inner browser does or what its callers see. It sits outside the recorded browser
/// on purpose: a replay of an old trial that never recorded a robots.txt must miss loudly, not pass quietly as allowed
/// (ADR 0049). It checks the page the browser is asked to open, not the images, scripts and frames that page loads;
/// those are what any visitor's browser loads for it.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0049-nothing-is-compatible-before-a-sale.md">ADR 0049: nothing is compatible before a sale</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0068-every-fetch-obeys-robots-txt.md">ADR 0068: every fetch obeys robots.txt</seealso>
public sealed class RobotsWebBrowser(IWebBrowser inner, RobotsPolicy robots) : IWebBrowser
{
    /// <summary>
    /// Opens the page if the site's robots.txt allows it, and hands it back unless the page says no to AI readers.
    /// </summary>
    public async Task<BrowserVisit> VisitAsync(Uri url, Func<Uri, bool>? capture = null, BrowserSession? session = null,
        string? script = null, string? readySelector = null, CancellationToken cancellationToken = default)
    {
        // Ask robots.txt first: a refused page never reaches Chrome.
        if (await robots.RefusalAsync(url, cancellationToken) is { } refusal) throw new WebBrowserException(refusal);

        var visit = await inner.VisitAsync(url, capture, session, script, readySelector, cancellationToken);

        // Then the page's own words: its meta tag, read from the HTML Chrome drew.
        var page = Uri.TryCreate(visit.Url, UriKind.Absolute, out var final) ? final : url;
        if (robots.PageRefusal(page, null, visit.Html) is { } pageRefusal)
            throw new WebBrowserException(pageRefusal);

        return visit;
    }
}
