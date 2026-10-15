namespace LouisAgent.Core.Agent;

using LouisAgent.Core.Config;
using LouisAgent.Core.Pricing;
using LouisAgent.Core.Tools.Web;
using LouisAgent.Core.Usage;

/// <summary>What a toolset needs to build its tools for one engine; <paramref name="Rates"/> are the ones the host loaded
/// for the display currency (F14), which <c>ConvertCurrency</c> uses too (F22); <paramref name="UsageSink"/> is the engine's
/// ledger, which the <c>agents</c> toolset writes a sub-session's roll-up to (F5); null when the ledger is off.</summary>
public sealed record ToolsetContext(AgentOptions Options, IWebSearchProvider WebSearch, ExchangeRates? Rates = null, IUsageSink? UsageSink = null,
    IPageRenderer? PageRenderer = null)
{
    /// <summary>The page renderer for the web toolset (F25): the one given, else the one configuration picks.</summary>
    public IPageRenderer Renderer => PageRenderer ?? PageRendererFactory.Create(Options);
}
