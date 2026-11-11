using LouisAgent.Composition.Packs;
using LouisAgent.Composition.Pricing;
using LouisAgent.Composition.Web.Browsing;
using LouisAgent.Composition.Web.Fetching;
using LouisAgent.Composition.Web.Rendering;
using LouisAgent.Composition.Web.Search;
using LouisAgent.Core.Config;
using LouisAgent.Core.Pricing.Rates;
using LouisAgent.Core.Sessions.Replay;
using LouisAgent.Core.Sessions.Replay.Store;
using LouisAgent.Core.Usage.Sinks;
using LouisAgent.Core.Web;

namespace LouisAgent.Core.Agent.Toolsets;

/// <summary>
/// What a toolset needs to build its tools for one engine: the settings, the web as the host gave it
/// (<see cref="IWebAccess"/>, CL33: the search provider, the browser if any, the renderer, a fetcher per call, all
/// under the host's robots policy and its replay store), <paramref name="Rates"/> as the host loaded them for the
/// display currency (F14), which <c>ConvertCurrency</c> uses too (F22), <paramref name="UsageSink"/> the engine's
/// ledger, which the <c>agents</c> toolset writes a sub-session's roll-up to (F5), null when the ledger is off, and
/// <paramref name="Replay"/> the process's one store of recorded answers (F18 step 4b), null where none is kept.
/// </summary>
public sealed record ToolsetContext(
    AgentOptions Options,
    IWebAccess Web,
    ExchangeRates? Rates = null,
    IUsageSink? UsageSink = null,
    ReplayStore? Replay = null) : IPackContext
{
    /// <inheritdoc/>
    public string WorkspaceRoot => Options.WorkspaceRoot;

    public ICurrencyRates? CurrencyRates => Rates;

    /// <inheritdoc/>
    public IWebSearchProvider WebSearch => Web.Search;

    /// <inheritdoc/>
    public IPageFetcher NewFetcher() => Web.NewFetcher();

    /// <inheritdoc/>
    public IPageRenderer Renderer => Web.Renderer;

    /// <inheritdoc/>
    public bool InATrial => SessionReplay.Current is { Web: not WebReplayMode.Off };

    /// <summary>The guarded browser (F33), one per process so every engine shares it and its recordings; none without Chrome.</summary>
    public IWebBrowser? Browser => Web.Browser;

    /// <summary>
    /// The identities this instance runs, which the <c>agents</c> toolset asks among (F127.3.1); none for a host
    /// given no identity.
    /// </summary>
    public IReadOnlyList<IAgentIdentity> Identities { get; init; } = [];
}
