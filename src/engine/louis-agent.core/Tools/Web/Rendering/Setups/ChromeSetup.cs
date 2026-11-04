using LouisAgent.Core.Config;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools.Web.Browsing;
using LouisAgent.Core.Tools.Web.Messages;
using LouisAgent.Core.Tools.Web.Rendering.Providers;

namespace LouisAgent.Core.Tools.Web.Rendering.Setups;

/// <summary>
/// Reads pages through our own headless Chrome. If we don't know where Chrome runs, you get a renderer that tells the model to set `BROWSER_URL`.
/// </summary>
/// <remarks>
/// Strategy, with a Null Object. Each renderer knows for itself whether it's set up and how to build itself, so "is it ready?" and "build it" can't drift apart,
/// and a new renderer is one new class instead of another case in two switches. When Chrome isn't set up it hands back a renderer that explains what's missing,
/// so nothing has to check for null and the model still gets a useful answer.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0038-the-browser-says-what-it-is.md">ADR 0038: the browser says what it is</seealso>
internal sealed class ChromeSetup : IPageRendererSetup
{
    /// <summary>This is the Chrome one.</summary>
    public PageRendererKind Kind => PageRendererKind.Chrome;

    /// <summary>We can only use Chrome if we know where it runs.</summary>
    public bool IsReady(AgentOptions options) => AddressOf(options) is not null;

    /// <summary>Builds the Chrome renderer, or the one that says to set the browser's address.</summary>
    public IPageRenderer Create(AgentOptions options, HttpClient? http, IWebBrowser? browser) =>
        AddressOf(options) is { } address
            ? new ChromePageRenderer(browser ?? new ChromeWebBrowser(address, userAgent: options.BrowserUserAgent))
            : new UnavailablePageRenderer(MessageText.Format(WebToolMessage.BrowserNotConfigured));

    /// <summary>Reads the browser's address, or nothing if it isn't a real URL.</summary>
    private static Uri? AddressOf(AgentOptions options) =>
        Uri.TryCreate(options.BrowserUrl?.Trim(), UriKind.Absolute, out var url) ? url : null;
}
