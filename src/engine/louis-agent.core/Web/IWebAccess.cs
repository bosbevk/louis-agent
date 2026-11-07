using LouisAgent.Composition.Web.Browsing;
using LouisAgent.Composition.Web.Fetching;
using LouisAgent.Composition.Web.Rendering;
using LouisAgent.Composition.Web.Search;

namespace LouisAgent.Core.Web;

/// <summary>
/// What the engine needs from the web, and all core knows of it (CL33): a search provider, a browser if the host has
/// one, a page renderer, and a fresh fetcher for each call, every one under the host's robots policy. The
/// implementations live in <c>louis-agent.core.web</c> (<c>WebAccess.Create</c>); a host hands them to
/// <c>AgentHost.Build</c> in its <c>HostServices</c>, and a pack reads them through its <c>IPackContext</c>.
/// </summary>
/// <remarks>
/// Separated Interface (Fowler): the interface lives with the code that needs it, the browser, the fetcher and the
/// providers with the library that has them, so core ships no browser binary and a host opts in. Without one the
/// engine runs on <see cref="NoWebAccess"/>, whose parts say web access isn't set up.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0076-core-ships-no-agent.md">ADR 0076: core ships no agent</seealso>
public interface IWebAccess
{
    /// <summary>The search provider the settings name, recorded for replay where a store is kept.</summary>
    IWebSearchProvider Search { get; }

    /// <summary>The guarded browser (F33), one per process so every engine shares it; none when the host has no Chrome.</summary>
    IWebBrowser? Browser { get; }

    /// <summary>The page renderer the settings pick, reading through the browser where it has one.</summary>
    IPageRenderer Renderer { get; }

    /// <summary>A fetcher for one call, under the process's robots policy and its replay store.</summary>
    IPageFetcher NewFetcher();
}
