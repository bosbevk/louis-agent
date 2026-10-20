namespace LouisAgent.Core.Tools.Web.Browsing;

using System.Net;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools.Web.Browsing.Policy;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;

/// <summary>
/// The headless Chrome we run (F32; a seam of its own since F33 step 1, so a shop's catalogue uses the same guards as the
/// renderer), driven over the DevTools protocol by PuppeteerSharp, which connects and downloads no browser. Per visit:
/// connect, a new page with every request it makes put to <see cref="RenderPolicy"/> and no WebSockets, the page loaded
/// (30 s at most) and given up to 10 s more for its scripts to fetch what they draw, the responses a caller asked for
/// kept, the page closed and the connection dropped. No connection is kept, so a browser restart costs nothing; at most
/// <see cref="MaxPages"/> pages are open at once across the process, so a cook's minions don't open a tab each. Chrome
/// answers its DevTools address only by IP or as localhost, so a service name (a compose stack's browser) is resolved first.
/// A visit with a session (the cookies it starts with) or a script (one of ours, run once the page has loaded) has a
/// browser context of its own, dropped after, and hands back the cookies the site set (F34): one area's store never
/// reaches another visit. A plain visit shares the browser's default context, as before.
/// </summary>
public sealed class ChromeWebBrowser(Uri browserUrl, Func<string, CancellationToken, Task<IPAddress[]>>? resolve = null) : IWebBrowser
{
    private const int MaxPages = 4;
    private static readonly SemaphoreSlim Pages = new(MaxPages, MaxPages);
    private static readonly TimeSpan Load = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan Settle = TimeSpan.FromSeconds(10);
    private static readonly ILogger Log = AgentLogging.For<ChromeWebBrowser>();

    /// <summary>Runs before a page's scripts in every frame: a WebSocket can't be made.</summary>
    private const string NoWebSockets =
        "Object.defineProperty(globalThis, 'WebSocket', { value: function () { throw new Error('WebSocket is blocked'); }, " +
        "writable: false, configurable: false });";

    private readonly Func<string, CancellationToken, Task<IPAddress[]>> _resolve = resolve ?? ((host, ct) => Dns.GetHostAddressesAsync(host, ct));

    public async Task<BrowserVisit> VisitAsync(Uri url, Func<Uri, bool>? capture = null, BrowserSession? session = null, string? script = null,
        string? readySelector = null, CancellationToken cancellationToken = default)
    {
        await Pages.WaitAsync(cancellationToken);
        try
        {
            IBrowser browser = await ConnectAsync(cancellationToken);
            try
            {
                return await VisitInAsync(browser, url, capture, session, script, readySelector, cancellationToken);
            }
            finally
            {
                browser.Disconnect();
            }
        }
        finally
        {
            Pages.Release();
        }
    }

    private async Task<IBrowser> ConnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await Puppeteer.ConnectAsync(new ConnectOptions { BrowserURL = (await ByAddressAsync(cancellationToken)).ToString().TrimEnd('/') });
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not OutOfMemoryException)
        {
            throw new WebBrowserException(MessageText.Format(ToolMessage.BrowserUnreachable, new { Url = browserUrl, Reason = ex.Message }));
        }
    }

    /// <summary>The browser's address with its host as an IP: Chrome refuses a Host header that is a name.</summary>
    private async Task<Uri> ByAddressAsync(CancellationToken cancellationToken)
    {
        if (browserUrl.IsLoopback || IPAddress.TryParse(browserUrl.Host.Trim('[', ']'), out _)) return browserUrl;
        var addresses = await Dns.GetHostAddressesAsync(browserUrl.DnsSafeHost, cancellationToken);
        var address = addresses.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) ?? addresses.First();
        return new UriBuilder(browserUrl) { Host = address.ToString() }.Uri;
    }

    private async Task<BrowserVisit> VisitInAsync(IBrowser browser, Uri url, Func<Uri, bool>? capture, BrowserSession? session,
        string? script, string? readySelector, CancellationToken cancellationToken)
    {
        IBrowserContext? own = session is not null || script is not null ? await browser.CreateBrowserContextAsync() : null;
        var page = own is null ? await browser.NewPageAsync() : await own.NewPageAsync();
        if (session is { Cookies.Count: > 0 })
            await page.SetCookieAsync([.. session.Cookies.Select(c => new CookieParam { Name = c.Name, Value = c.Value, Domain = c.Domain, Path = "/" })]);
        // A cancelled turn closes its page, which ends the load or the wait at once and frees its slot (review of PR #56:
        // PuppeteerSharp's waits take no token, so a cancelled render held a page for up to 40 s).
        using var closeOnCancel = cancellationToken.Register(() => _ = CloseQuietlyAsync(page));
        try
        {
            return await LoadAsync(page, url, capture, script, readySelector, own is not null);
        }
        catch (Exception ex) when (cancellationToken.IsCancellationRequested && ex is not OperationCanceledException)
        {
            throw new OperationCanceledException(null, ex, cancellationToken);
        }
        finally
        {
            await CloseQuietlyAsync(page);
            if (own is not null)
                try
                {
                    await own.CloseAsync();
                }
                catch (Exception ex) when (ex is not OutOfMemoryException)
                {
                    // The browser went away, and the context with it.
                }
        }
    }

    /// <summary>Closes a page that may already be closing (cancelled) or gone with its connection; never throws.</summary>
    private static async Task CloseQuietlyAsync(IPage page)
    {
        try
        {
            if (!page.IsClosed) await page.CloseAsync();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Closed already, or the browser went away: either way the page is gone.
        }
    }

    private async Task<BrowserVisit> LoadAsync(IPage page, Uri url, Func<Uri, bool>? capture, string? script, string? readySelector, bool ownContext)
    {
        string? refusedPage = null;
        // Request interception doesn't see WebSockets (nor does Chrome's URL blocking), and the browser shares a network
        // with a product's app (F32 step 4), so a page could open one to an internal address. No shop needs one for its prices:
        // before any of the page's own scripts, in every frame, WebSocket refuses to be made.
        await page.EvaluateExpressionOnNewDocumentAsync(NoWebSockets);
        await page.SetRequestInterceptionAsync(true);
        // An event handler is async void: an exception escaping it would end the process, every open chat with it
        // (review of PR #56), so nothing may. A request that can't be decided or answered is ended, never made.
        page.Request += async (_, e) =>
        {
            var request = e.Request;
            bool answered = false;
            try
            {
                var verdict = Uri.TryCreate(request.Url, UriKind.Absolute, out var target)
                    ? await RenderPolicy.DecideAsync(target, request.ResourceType, _resolve)
                    : RenderVerdict.Refuse;
                if (verdict == RenderVerdict.Allow)
                {
                    await request.ContinueAsync();
                    answered = true;
                    return;
                }
                if (verdict == RenderVerdict.Refuse)
                {
                    if (request.IsNavigationRequest && request.Frame == page.MainFrame) refusedPage = request.Url;
                    Log.Log(LogLevel.Warning, LogMessage.RenderRequestRefused, new { Request = request.Url, Page = url });
                }
                await request.AbortAsync(RequestAbortErrorCode.BlockedByClient);
                answered = true;
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                Log.Log(LogLevel.Warning, LogMessage.RenderRequestUnhandled, new { Request = request.Url, Page = url, Reason = ex.Message });
                // Not answered yet: ended now, not left until the page's timeout (re-review of PR #56). Not made either.
                if (!answered)
                    try
                    {
                        await request.AbortAsync(RequestAbortErrorCode.Failed);
                    }
                    catch (Exception again) when (again is not OutOfMemoryException)
                    {
                        // The page is gone, and the request with it.
                    }
            }
        };

        // The responses the caller asked for (a shop's catalogue answer): each body read by a task of its own, all of
        // them awaited before the visit ends, so none is lost to the page closing and no handler is async void.
        var captured = new List<string>();
        var reads = new List<Task>();
        var answered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        if (capture is not null)
            page.Response += (_, e) =>
            {
                if (!Uri.TryCreate(e.Response.Url, UriKind.Absolute, out var responseUrl) || !capture(responseUrl)) return;
                lock (reads) reads.Add(KeepAsync(e.Response, captured, answered));
            };

        try
        {
            await page.GoToAsync(url.AbsoluteUri, new NavigationOptions
            {
                Timeout = (int)Load.TotalMilliseconds, WaitUntil = [WaitUntilNavigation.DOMContentLoaded],
            });
        }
        catch (Exception ex) when (ex is NavigationException or TimeoutException)
        {
            string reason = refusedPage is { } address
                ? MessageText.Format(ToolMessage.RenderBlockedAddress, new { Address = address })
                : ex.Message;
            throw new WebBrowserException(MessageText.Format(ToolMessage.RenderFailed, new { Url = url, Reason = reason }));
        }

        if (capture is not null)
            await AnsweredAsync(page, answered.Task, readySelector);
        else
            try
            {
                await page.WaitForNetworkIdleAsync(new WaitForNetworkIdleOptions { Timeout = (int)Settle.TotalMilliseconds, IdleTime = 500 });
            }
            catch (TimeoutException)
            {
                // A page that never goes quiet (adverts, polling) is read as it stands.
            }

        // One script of ours, run once the page has loaded: its requests go through the same interception, so the
        // address rule holds for them; it answers a string (awaited when it's a promise).
        string? scriptResult = null;
        if (script is not null)
            try
            {
                scriptResult = await page.EvaluateExpressionAsync<string>(script);
            }
            catch (EvaluationFailedException ex)
            {
                throw new WebBrowserException(MessageText.Format(ToolMessage.BrowserScriptFailed, new { Url = url, Reason = ex.Message }));
            }

        Task[] pending;
        lock (reads) pending = [.. reads];
        await Task.WhenAll(pending);
        string html = await page.GetContentAsync();
        // The cookies the site set, only from a context of the visit's own: the default context's are other visits'.
        IReadOnlyList<BrowserCookie>? cookies = ownContext
            ? [.. (await page.GetCookiesAsync()).Select(c => new BrowserCookie(c.Name, c.Value, c.Domain))]
            : null;
        lock (captured) return new BrowserVisit(page.Url, html, [.. captured], scriptResult, cookies);
    }

    /// <summary>
    /// A visit that captures is done when it has its answer and, when given, the element that shows it is drawn (F34,
    /// Louis: "what causes this long delay?"): a shop's page never goes quiet, so waiting for quiet cost every search
    /// the full <see cref="Settle"/>. Neither comes: the visit ends at <see cref="Settle"/>, read as it stands.
    /// </summary>
    private static async Task AnsweredAsync(IPage page, Task answered, string? readySelector)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        if (await Task.WhenAny(answered, Task.Delay(Settle)) != answered || readySelector is null) return;
        int left = (int)Math.Max(0, (Settle - watch.Elapsed).TotalMilliseconds);
        try
        {
            await page.WaitForSelectorAsync(readySelector, new WaitForSelectorOptions { Timeout = left });
        }
        catch (Exception ex) when (ex is WaitTaskTimeoutException or TimeoutException)
        {
            // The answer came but nothing showed it in time (an empty search): read as it stands.
        }
    }

    /// <summary>Keeps one captured response's body; one that can't be read (the page moved on) is logged and left out.</summary>
    private static async Task KeepAsync(IResponse response, List<string> captured, TaskCompletionSource answered)
    {
        try
        {
            string body = await response.TextAsync();
            lock (captured) captured.Add(body);
            answered.TrySetResult();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            Log.Log(LogLevel.Warning, LogMessage.RenderRequestUnhandled, new { Request = response.Url, Page = response.Url, Reason = ex.Message });
        }
    }
}
