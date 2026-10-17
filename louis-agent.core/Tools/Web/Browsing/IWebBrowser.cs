namespace LouisAgent.Core.Tools.Web.Browsing;

/// <summary>
/// The guarded headless browser (F33 step 1, from F32's renderer): every user of a browser goes through it, so its
/// guards live in one place. A visit loads a page with every request it makes put to <see cref="RenderPolicy"/> and no
/// WebSockets, keeps the bodies of the responses <c>capture</c> picks (the page's own data, as a shop's catalogue), and
/// ends at once when cancelled. Problems a user can act on throw <see cref="WebBrowserException"/>.
/// </summary>
public interface IWebBrowser
{
    Task<BrowserVisit> VisitAsync(Uri url, Func<Uri, bool>? capture = null, CancellationToken cancellationToken = default);
}
