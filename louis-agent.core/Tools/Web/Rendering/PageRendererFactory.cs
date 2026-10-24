namespace LouisAgent.Core.Tools.Web.Rendering;

using LouisAgent.Core.Config;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools.Web.Browsing;
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

    public static IPageRenderer Create(AgentOptions options, HttpClient? http = null)
    {
        bool hasTavily = !string.IsNullOrWhiteSpace(options.TavilyApiKey);
        Uri? browser = Uri.TryCreate(options.BrowserUrl?.Trim(), UriKind.Absolute, out var url) ? url : null;
        if (!SettingChoice.TryParse(options.PageRenderer, out PageRendererKind? choice))
        {
            string named = options.PageRenderer!.Trim();
            return new UnavailablePageRenderer(MessageText.Format(ToolMessage.UnknownPageRenderer,
                new { Renderer = named, Supported = string.Join(", ", SupportedRenderers) }));
        }

        return choice switch
        {
            null when browser is not null => new ChromePageRenderer(new ChromeWebBrowser(browser, userAgent: options.BrowserUserAgent)),
            null when hasTavily => new TavilyPageRenderer(options.TavilyApiKey!, http),
            null => new UnavailablePageRenderer(MessageText.Format(ToolMessage.RenderNotConfigured)),
            PageRendererKind.Chrome => browser is not null
                ? new ChromePageRenderer(new ChromeWebBrowser(browser, userAgent: options.BrowserUserAgent))
                : new UnavailablePageRenderer(MessageText.Format(ToolMessage.BrowserNotConfigured)),
            PageRendererKind.Tavily => hasTavily
                ? new TavilyPageRenderer(options.TavilyApiKey!, http)
                : new UnavailablePageRenderer(MessageText.Format(ToolMessage.RenderNotConfigured)),
            _ => throw new ArgumentOutOfRangeException(nameof(options), choice, null),
        };
    }
}
