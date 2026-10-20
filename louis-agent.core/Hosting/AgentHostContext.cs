namespace LouisAgent.Core.Hosting;

using LouisAgent.Core.Pricing.Prices;
using LouisAgent.Core.Pricing.Rates;
using dotenv.net;
using LouisAgent.Core.Config;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Hosting;

/// <summary>Everything an entry point needs after bootstrap.</summary>
/// <param name="Prices">The price table the usage ledger prices with (louis-agent.api also serves it at GET /prices).</param>
/// <param name="Rates">Exchange rates from the price table's currency, for showing costs in another currency (F14).</param>
/// <param name="Display">The currency costs are shown in (F14): HOST_CULTURE's region, DISPLAY_CURRENCY, or as charged.</param>
/// <param name="Engine">The host's own engine: every toolset this instance can load (CLI, ACP server).</param>
/// <param name="Engines">One engine per toolset profile (F5), for hosts whose sessions choose their toolsets (the API).</param>
public sealed record AgentHostContext(
    AgentEngine Engine, LlmOptions Llm, AgentOptions Agent, PriceTable Prices, ExchangeRates Rates, DisplayCurrency Display,
    AgentEngineFactory Engines);
