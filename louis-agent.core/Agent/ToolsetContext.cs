namespace LouisAgent.Core.Agent;

using LouisAgent.Core.Config;
using LouisAgent.Core.Pricing;
using LouisAgent.Core.Tools.Web;

/// <summary>What a toolset needs to build its tools for one engine; <paramref name="Rates"/> are the ones the host loaded
/// for the display currency (F14), which <c>ConvertCurrency</c> uses too (F22).</summary>
public sealed record ToolsetContext(AgentOptions Options, IWebSearchProvider WebSearch, ExchangeRates? Rates = null);
