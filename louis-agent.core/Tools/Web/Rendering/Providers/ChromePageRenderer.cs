namespace LouisAgent.Core.Tools.Web.Rendering.Providers;

using System.Net;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools.Web.Rendering;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;

/// <summary>
/// A page rendered by the headless Chrome we run (F32 step 2; Louis: "ReadRenderedPage we need this for all pages from
/// all providers"), driven over the DevTools protocol by PuppeteerSharp, which connects and downloads no browser.
/// Per call: connect, a new page with every request it makes put to <see cref="RenderPolicy"/>, the page loaded (30 s at
/// most) and given up to 10 s more for its scripts to fetch what they draw, its HTML read the way
/// <see cref="WebTools.FetchUrl"/> reads HTML, the page closed and the connection dropped. No connection is kept, so a
/// browser restart costs nothing; at most <see cref="MaxPages"/> pages are open at once across the process, so a cook's
/// minions don't open a tab each. Chrome answers its DevTools address only by IP or as localhost, so a service name
/// (puls-browser) is resolved first.
/// </summary>
public sealed class ChromePageRenderer(Uri browserUrl, Func<string, CancellationToken, Task<IPAddress[]>>? resolve = null) : IPageRenderer
{
    private const int MaxPages = 4;
    private static readonly SemaphoreSlim Pages = new(MaxPages, MaxPages);
    private static readonly TimeSpan Load = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan Settle = TimeSpan.FromSeconds(10);
    private static readonly ILogger Log = AgentLogging.For<ChromePageRenderer>();

    /// <summary>Runs before a page's scripts in every frame: a WebSocket can't be made.</summary>
    private const string NoWebSockets =
        "Object.defineProperty(globalThis, 'WebSocket', { value: function () { throw new Error('WebSocket is blocked'); }, " +
        "writable: false, configurable: false });";

    private readonly Func<string, CancellationToken, Task<IPAddress[]>> _resolve = resolve ?? ((host, ct) => Dns.GetHostAddressesAsync(host, ct));

    public string Name => "Chrome";

    public async Task<RenderedPage> RenderAsync(Uri url, CancellationToken cancellationToken = default)
    {
        await Pages.WaitAsync(cancellationToken);
        try
        {
            IBrowser browser = await ConnectAsync(cancellationToken);
            try
            {
                return await RenderInAsync(browser, url);
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
            throw new PageRenderException(MessageText.Format(ToolMessage.BrowserUnreachable, new { Url = browserUrl, Reason = ex.Message }));
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

    private async Task<RenderedPage> RenderInAsync(IBrowser browser, Uri url)
    {
        await using var page = await browser.NewPageAsync();
        string? refusedPage = null;
        // Request interception doesn't see WebSockets (nor does Chrome's URL blocking), and the browser shares a network
        // with puls (step 4), so a page could open one to an internal address. No shop needs one for its prices: before
        // any of the page's own scripts, in every frame, WebSocket refuses to be made.
        await page.EvaluateExpressionOnNewDocumentAsync(NoWebSockets);
        await page.SetRequestInterceptionAsync(true);
        page.Request += async (_, e) =>
        {
            var request = e.Request;
            var verdict = Uri.TryCreate(request.Url, UriKind.Absolute, out var target)
                ? await RenderPolicy.DecideAsync(target, request.ResourceType, _resolve)
                : RenderVerdict.Refuse;
            if (verdict == RenderVerdict.Allow)
            {
                await request.ContinueAsync();
                return;
            }
            if (verdict == RenderVerdict.Refuse)
            {
                if (request.IsNavigationRequest && request.Frame == page.MainFrame) refusedPage = request.Url;
                Log.Log(LogLevel.Warning, LogMessage.RenderRequestRefused, new { Request = request.Url, Page = url });
            }
            await request.AbortAsync(RequestAbortErrorCode.BlockedByClient);
        };

        try
        {
            await page.GoToAsync(url.ToString(), new NavigationOptions
            {
                Timeout = (int)Load.TotalMilliseconds, WaitUntil = [WaitUntilNavigation.DOMContentLoaded],
            });
        }
        catch (Exception ex) when (ex is NavigationException or TimeoutException)
        {
            string reason = refusedPage is { } address
                ? MessageText.Format(ToolMessage.RenderBlockedAddress, new { Address = address })
                : ex.Message;
            throw new PageRenderException(MessageText.Format(ToolMessage.RenderFailed, new { Url = url, Reason = reason }));
        }

        try
        {
            await page.WaitForNetworkIdleAsync(new WaitForNetworkIdleOptions { Timeout = (int)Settle.TotalMilliseconds, IdleTime = 500 });
        }
        catch (TimeoutException)
        {
            // A page that never goes quiet (adverts, polling) is read as it stands.
        }

        var (title, text) = WebTools.HtmlToText(await page.GetContentAsync(), new Uri(page.Url));
        return new RenderedPage(page.Url, string.IsNullOrWhiteSpace(title) ? text : $"# {title}\n\n{text}");
    }
}
