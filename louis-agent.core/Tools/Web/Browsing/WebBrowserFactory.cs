namespace LouisAgent.Core.Tools.Web.Browsing;

using LouisAgent.Core.Config;
using LouisAgent.Core.Tools.Web.Replay;
using LouisAgent.Core.Tools.Web.Replay.Store;

/// <summary>
/// Builds the process's guarded browser from configuration (F18 step 4a): none without a usable <c>BROWSER_URL</c>; otherwise the
/// headless Chrome with the user agent <c>BROWSER_USER_AGENT</c> names (FX21), wrapped in a recording browser with one store when
/// <c>REPLAY_DIRECTORY</c> is set. Built once and handed to everything that reads a page (the shops and the page renderer), so they
/// share one browser and one store; it sits beside the browser as the renderer's and the search provider's factories sit by theirs.
/// </summary>
public static class WebBrowserFactory
{
    public static IWebBrowser? Create(AgentOptions options)
    {
        if (!Uri.TryCreate(options.BrowserUrl?.Trim(), UriKind.Absolute, out var url)) return null;
        var chrome = new ChromeWebBrowser(url, userAgent: options.BrowserUserAgent);
        return options.ReplayDirectory is { } directory ? new RecordedWebBrowser(chrome, new WebReplayStore(directory)) : chrome;
    }
}
