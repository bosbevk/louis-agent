namespace LouisAgent.Core.Tools.Web.Rendering;

using LouisAgent.Core.Config;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools.Web.Browsing;
using LouisAgent.Core.Tools.Web.Messages;
using LouisAgent.Core.Tools.Web.Rendering.Providers;

/// <summary>
/// Picks the page renderer from configuration: PAGE_RENDERER = chrome | tavily (F32; an enum, ADR 0024). When unset, the
/// headless Chrome we run is used if BROWSER_URL is set, else Tavily's Extract if TAVILY_API_KEY is (the same key as its
/// search), otherwise a renderer whose every call says what to set. Misconfiguration never fails start-up.
/// </summary>
public static class PageRendererFactory
{
    /// <summary>The names <c>PAGE_RENDERER</c> takes, from <see cref="PageRendererKind"/>.</summary>
    public static IReadOnlyList<string> SupportedRenderers { get; } = SettingChoice.Names<PageRendererKind>();

    /// <param name="browser">The process's one guarded browser (F18 step 4a: the shops read through it too, so a recorded run holds every page); the Chrome renderer builds its own only when none is given.</param>
    public static IPageRenderer Create(AgentOptions options, HttpClient? http = null, IWebBrowser? browser = null)
    {
        bool hasTavily = !string.IsNullOrWhiteSpace(options.TavilyApiKey);
        Uri? browserUrl = Uri.TryCreate(options.BrowserUrl?.Trim(), UriKind.Absolute, out var url) ? url : null;
        if (!SettingChoice.TryParse(options.PageRenderer, out PageRendererKind? choice))
        {
            string named = options.PageRenderer!.Trim();
            return new UnavailablePageRenderer(MessageText.Format(WebToolMessage.UnknownPageRenderer,
                new { Renderer = named, Supported = string.Join(", ", SupportedRenderers) }));
        }

        return choice switch
        {
            null when browserUrl is not null => new ChromePageRenderer(browser ?? new ChromeWebBrowser(browserUrl, userAgent: options.BrowserUserAgent)),
            null when hasTavily => new TavilyPageRenderer(options.TavilyApiKey!, http),
            null => new UnavailablePageRenderer(MessageText.Format(WebToolMessage.RenderNotConfigured)),
            PageRendererKind.Chrome => browserUrl is not null
                ? new ChromePageRenderer(browser ?? new ChromeWebBrowser(browserUrl, userAgent: options.BrowserUserAgent))
                : new UnavailablePageRenderer(MessageText.Format(WebToolMessage.BrowserNotConfigured)),
            PageRendererKind.Tavily => hasTavily
                ? new TavilyPageRenderer(options.TavilyApiKey!, http)
                : new UnavailablePageRenderer(MessageText.Format(WebToolMessage.RenderNotConfigured)),
            _ => throw new ArgumentOutOfRangeException(nameof(options), choice, null),
        };
    }
}
