using LouisAgent.Composition.Web.Browsing;
using LouisAgent.Composition.Web.Fetching;
using LouisAgent.Composition.Web.Rendering;
using LouisAgent.Composition.Web.Search;
using LouisAgent.Core.Config;
using LouisAgent.Core.Sessions.Replay.Store;
using LouisAgent.Core.Web.Browsing;
using LouisAgent.Core.Web.Fetching;
using LouisAgent.Core.Web.Rendering;
using LouisAgent.Core.Web.Replay;
using LouisAgent.Core.Web.Robots;
using LouisAgent.Core.Web.Search;

namespace LouisAgent.Core.Web;

/// <summary>
/// The web as this library gives it to the engine (CL33): one robots.txt policy for the process (F35, ADR 0068),
/// one guarded browser from <c>BROWSER_URL</c> if there is one (F33), the search provider the settings name, wrapped
/// by the recorder where a replay store is kept (F18 step 4b), the page renderer the settings pick, reading through
/// the browser, and a fetcher per call under the policy and the store. A host hands <see cref="Create(AgentOptions, ReplayStore?)"/>
/// to <c>AgentHost.Build</c> in its <c>HostServices</c>.
/// </summary>
/// <remarks>
/// The real side of core's <see cref="IWebAccess"/> seam (Separated Interface); what <c>AgentEngineFactory</c> built
/// itself before, moved here whole so core ships no browser binary.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0068-every-fetch-obeys-robots-txt.md">ADR 0068: every fetch obeys robots.txt</seealso>
public sealed class WebAccess : IWebAccess
{
    private readonly AgentOptions _options;
    private readonly ReplayStore? _replay;
    private readonly RobotsPolicy _robots;

    private WebAccess(AgentOptions options, ReplayStore? replay, IWebSearchProvider search)
    {
        _options = options;
        _replay = replay;
        // The policy reads robots.txt through a fetcher of its own that has no policy.
        _robots = new RobotsPolicy(RobotsFileFetch.Through(new PageFetcher(replay: replay)));
        Search = RecordedSearchProvider.Around(search, replay);
        Browser = WebBrowserFactory.Create(options, replay, _robots);
    }

    /// <summary>The web from the settings and the store, the search provider the settings name.</summary>
    public static IWebAccess Create(AgentOptions options, ReplayStore? replay = null) =>
        new WebAccess(options, replay, WebSearchProviderFactory.Create(options));

    /// <summary>The same, with a search provider of the host's own (a test's fake, recorded like a real one).</summary>
    public static IWebAccess Create(AgentOptions options, ReplayStore? replay, IWebSearchProvider search) =>
        new WebAccess(options, replay, search);

    /// <summary>The provider the settings name, recorded where a store is kept.</summary>
    public IWebSearchProvider Search { get; }

    /// <summary>The one guarded browser from <c>BROWSER_URL</c>; none without Chrome.</summary>
    public IWebBrowser? Browser { get; }

    /// <summary>The renderer the settings pick, made for each ask as before, over the one browser.</summary>
    public IPageRenderer Renderer => PageRendererFactory.Create(_options, browser: Browser);

    /// <summary>A fetcher for one call, under the process's robots policy and its store.</summary>
    public IPageFetcher NewFetcher() => new PageFetcher(replay: _replay, robots: _robots);
}
