namespace LouisAgent.Core.Pricing.Prices;

using LouisAgent.Core.Config;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Pricing.Messages;
using Microsoft.Extensions.Logging;

/// <summary>The price table a host loads at start-up (S, CL15, from AgentHost): from a URL when it answers, else the file.</summary>
public static class PriceTableLoader
{
    private static readonly ILogger Log = AgentLogging.For<PriceTable>();

    private static readonly TimeSpan PricesUrlTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// The price table: from PRICES_URL when set and reachable, otherwise from PRICES_FILE or config/prices.json (found like
    /// config/.env); empty, with a warning, if there's none. A response or file that isn't a valid table throws.
    /// </summary>
    /// <param name="handler">For tests: answers the PRICES_URL request.</param>
    public static PriceTable Load(AgentOptions agent, HttpMessageHandler? handler = null)
    {
        if (agent.PricesUrl is { } pricesUrl)
        {
            if (!Uri.TryCreate(pricesUrl, UriKind.Absolute, out Uri? url) || url.Scheme is not ("http" or "https"))
                throw new InvalidDataException(MessageText.Format(PricingErrorMessage.PricesUrlInvalid, new { Url = pricesUrl }));

            using var http = handler is null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
            http.Timeout = PricesUrlTimeout;
            try
            {
                // Start-up is synchronous in every host; the call is bounded by the timeout above.
                PriceTable fromUrl = PriceTable.LoadAsync(http, url).GetAwaiter().GetResult();
                Log.Log(LogLevel.Information, PricingLogMessage.PricesLoaded, new { Source = url, fromUrl.AsOf, fromUrl.Models.Count });
                return fromUrl;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                // Pricing being unreachable never stops an agent: the local table keeps costs flowing.
                Log.Log(LogLevel.Warning, PricingLogMessage.PricesUrlUnreachable, new { Url = url, Reason = ex.Message });
            }
        }

        string? path = agent.PricesFile
                       ?? (EnvFile.FindConfigDirectory(Environment.CurrentDirectory) is { } configDir ? Path.Combine(configDir, "prices.json") : null);
        PriceTable prices = path is null ? PriceTable.Empty : PriceTable.Load(path);
        if (prices.IsEmpty)
            Log.Log(LogLevel.Warning, PricingLogMessage.NoPriceTable, new { Path = path ?? "config/prices.json" });
        else
            Log.Log(LogLevel.Information, PricingLogMessage.PricesLoaded, new { Source = path, prices.AsOf, prices.Models.Count });
        return prices;
    }
}
