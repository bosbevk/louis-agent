namespace LouisAgent.Core.Agent.Toolsets;

using LouisAgent.Core.Pricing.Rates;
using LouisAgent.Core.Usage.Sinks;
using LouisAgent.Core.Config;
using LouisAgent.Core.Tools.Web.Search;
using LouisAgent.Core.Tools.Web.Browsing;
using LouisAgent.Core.Tools.Web.Rendering;

/// <summary>What a toolset needs to build its tools for one engine; <paramref name="Rates"/> are the ones the host loaded
/// for the display currency (F14), which <c>ConvertCurrency</c> uses too (F22); <paramref name="UsageSink"/> is the engine's
/// ledger, which the <c>agents</c> toolset writes a sub-session's roll-up to (F5); null when the ledger is off.</summary>
public sealed record ToolsetContext(AgentOptions Options, IWebSearchProvider WebSearch, ExchangeRates? Rates = null, IUsageSink? UsageSink = null,
    IPageRenderer? PageRenderer = null, IWebBrowser? WebBrowser = null)
{
    /// <summary>The page renderer for the web toolset (F25): the one given, else the one configuration picks.</summary>
    public IPageRenderer Renderer => PageRenderer ?? PageRendererFactory.Create(Options);

    /// <summary>
    /// The guarded browser (F33): the one given, else the headless Chrome <c>BROWSER_URL</c> names, else none, and a toolset
    /// that needs one is unavailable.
    /// </summary>
    public IWebBrowser? Browser => WebBrowser
        ?? (Uri.TryCreate(Options.BrowserUrl?.Trim(), UriKind.Absolute, out var url) ? new ChromeWebBrowser(url, userAgent: Options.BrowserUserAgent) : null);
}
