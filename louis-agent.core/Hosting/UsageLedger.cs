
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting.Messages;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Pricing.Prices;
using LouisAgent.Core.Usage.Sinks;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Core.Hosting;
/// <summary>Where a host's usage records go (ADR 0007; S, CL15, from AgentHost).</summary>
public static class UsageLedger
{
    private static readonly ILogger Log = AgentLogging.For<PricingUsageSink>();

    /// <summary>
    /// The usage ledger, {LOG_DIRECTORY}/usage-YYYY-MM.jsonl, with each record priced from the price table; or null when
    /// USAGE_LEDGER=off or there is no LOG_DIRECTORY (with a warning, since the ledger was wanted). A malformed price table
    /// throws, so a host doesn't start on wrong prices.
    /// </summary>
    /// <param name="prices">Already loaded prices (<see cref="AgentHost.Build(LlmOptions, AgentOptions, ILlmClientFactory, AgentCatalog)"/> loads them once); null loads them here.</param>
    public static IUsageSink? Open(AgentOptions agent, PriceTable? prices = null)
    {
        if (!agent.UsageLedger) return null;
        if (agent.LogDirectory is null)
        {
            Log.Log(LogLevel.Warning, HostingLogMessage.LedgerOff);
            return null;
        }

        Log.Log(LogLevel.Information, HostingLogMessage.LedgerAt,
            new { Path = Path.Combine(agent.LogDirectory, "usage-YYYY-MM.jsonl") });
        return new PricingUsageSink(new JsonlUsageSink(agent.LogDirectory), prices ?? PriceTableLoader.Load(agent));
    }
}
