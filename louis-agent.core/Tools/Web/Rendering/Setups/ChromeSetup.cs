using LouisAgent.Core.Config;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools.Web.Browsing;
using LouisAgent.Core.Tools.Web.Messages;
using LouisAgent.Core.Tools.Web.Rendering.Providers;

namespace LouisAgent.Core.Tools.Web.Rendering.Setups;

/// <summary>Strategy: the headless Chrome we run. Ready with a valid <c>BROWSER_URL</c>; otherwise <see cref="Create"/> returns its Null Object, an unavailable renderer that says to set it.</summary>
internal sealed class ChromeSetup : IPageRendererSetup
{
    /// <summary>Chrome.</summary>
    public PageRendererKind Kind => PageRendererKind.Chrome;

    /// <summary>True when the browser address is set and is an absolute URL.</summary>
    public bool IsReady(AgentOptions options) => AddressOf(options) is not null;

    /// <summary>The Chrome renderer (on the given guarded browser, else one of its own) when ready; otherwise the Null Object that says the browser address is missing.</summary>
    public IPageRenderer Create(AgentOptions options, HttpClient? http, IWebBrowser? browser) =>
        AddressOf(options) is { } address
            ? new ChromePageRenderer(browser ?? new ChromeWebBrowser(address, userAgent: options.BrowserUserAgent))
            : new UnavailablePageRenderer(MessageText.Format(WebToolMessage.BrowserNotConfigured));

    /// <summary>The configured browser address as a URL, or null when it is missing or not an absolute URL.</summary>
    private static Uri? AddressOf(AgentOptions options) => Uri.TryCreate(options.BrowserUrl?.Trim(), UriKind.Absolute, out var url) ? url : null;
}
