namespace LouisAgent.Core.Agent.Toolsets;

using LouisAgent.Core.Pricing.Rates;
using LouisAgent.Core.Usage.Sinks;
using LouisAgent.Core.Config;
using LouisAgent.Core.Tools.Web.Search;
using LouisAgent.Core.Tools.Web.Browsing;
using LouisAgent.Core.Tools.Web.Rendering;
using LouisAgent.Core.Sessions.Replay.Store;

/// <summary>What a toolset needs to build its tools for one engine; <paramref name="Rates"/> are the ones the host loaded
/// for the display currency (F14), which <c>ConvertCurrency</c> uses too (F22); <paramref name="UsageSink"/> is the engine's
/// ledger, which the <c>agents</c> toolset writes a sub-session's roll-up to (F5); null when the ledger is off. <paramref name="Replay"/> is the
/// process's one store of recorded web answers (F18 step 4b); null where none is kept.</summary>
public sealed record ToolsetContext(AgentOptions Options, IWebSearchProvider WebSearch, ExchangeRates? Rates = null, IUsageSink? UsageSink = null,
    IPageRenderer? PageRenderer = null, IWebBrowser? WebBrowser = null, ReplayStore? Replay = null)
{
    /// <summary>The page renderer for the web toolset (F25): the one given, else the one configuration picks, reading through <see cref="Browser"/> (F18 step 4a).</summary>
    public IPageRenderer Renderer => PageRenderer ?? PageRendererFactory.Create(Options, browser: Browser);

    /// <summary>
    /// The guarded browser (F33): the one given (a host builds one per process, so every engine shares it and its recordings), else
    /// what <see cref="WebBrowserFactory"/> builds from <c>BROWSER_URL</c>, else none, and a toolset that needs one is unavailable.
    /// </summary>
    public IWebBrowser? Browser => WebBrowser ?? WebBrowserFactory.Create(Options, Replay);
}
