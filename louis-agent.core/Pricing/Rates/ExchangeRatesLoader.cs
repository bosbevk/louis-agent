namespace LouisAgent.Core.Pricing.Rates;

using LouisAgent.Core.Config;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Pricing.Messages;
using LouisAgent.Core.Pricing.Rates.Mappers;
using Microsoft.Extensions.Logging;

/// <summary>The exchange rates a host loads at start-up (F14; S, CL15, from AgentHost): from a URL when its reply is usable, else the file, from the right base.</summary>
public static class ExchangeRatesLoader
{
    private static readonly ILogger Log = AgentLogging.For<ExchangeRates>();

    private static readonly TimeSpan RatesUrlTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Exchange rates for showing costs in another currency (F14): from RATES_URL (Frankfurter by default), turned into our
    /// rates.json format by RATES_FORMAT's mapper; if that can't be fetched or read, from RATES_FILE or config/rates.json;
    /// empty, with a warning, if there's neither. Failures are sorted by who can fix them: anything wrong with RATES_URL's
    /// reply (down, a new shape, or rates from another currency) is the provider's, so it warns and uses the file; a rates
    /// file that isn't valid, or whose base isn't <paramref name="baseCurrency"/>, is our own configuration, so it throws:
    /// costs converted at a wrong rate are worse than not starting.
    /// </summary>
    /// <param name="baseCurrency">The price table's currency; the rates must be from it.</param>
    /// <param name="handler">For tests: answers the RATES_URL request.</param>
    public static ExchangeRates Load(AgentOptions agent, string baseCurrency = "USD", HttpMessageHandler? handler = null)
    {
        IRatesMapper mapper = RatesMappers.For(agent.RatesFormat);
        if (agent.RatesUrl is { } ratesUrl)
        {
            if (!Uri.TryCreate(ratesUrl, UriKind.Absolute, out Uri? url) || url.Scheme is not ("http" or "https"))
                throw new InvalidDataException(MessageText.Format(PricingErrorMessage.RatesUrlInvalid, new { Url = ratesUrl }));

            using var http = handler is null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
            http.Timeout = RatesUrlTimeout;
            try
            {
                // Start-up is synchronous in every host; the call is bounded by the timeout above.
                ExchangeRates fromUrl = ExchangeRates.LoadAsync(http, url, mapper).GetAwaiter().GetResult();
                CheckBase(fromUrl, baseCurrency, url.ToString());
                Log.Log(LogLevel.Information, PricingLogMessage.RatesLoaded, new { Source = url, fromUrl.AsOf, fromUrl.Rates.Count });
                return fromUrl;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidDataException or WrongBaseException)
            {
                // A third party's reply changing shape, its currency, or being down, never stops an agent: the rates file keeps
                // working. (Frankfurter's v2 reads ?from= as a date, so a v1 alias dropped would answer in EUR.)
                Log.Log(LogLevel.Warning, PricingLogMessage.RatesUrlUnusable, new { Url = url, Reason = ex.Message });
            }
        }

        string? path = agent.RatesFile
                       ?? (EnvFile.FindConfigDirectory(Environment.CurrentDirectory) is { } configDir ? Path.Combine(configDir, "rates.json") : null);
        ExchangeRates rates = path is null ? ExchangeRates.Empty : ExchangeRates.Load(path);
        if (rates.IsEmpty)
        {
            Log.Log(LogLevel.Warning, PricingLogMessage.NoRates, new { Path = path ?? "config/rates.json", Base = baseCurrency });
            return rates;
        }

        CheckBase(rates, baseCurrency, path!);
        Log.Log(LogLevel.Information, PricingLogMessage.RatesLoaded, new { Source = path, rates.AsOf, rates.Rates.Count });
        return rates;
    }

    /// <summary>Rates from another currency than the price table's would convert every cost wrongly.</summary>
    private static void CheckBase(ExchangeRates rates, string baseCurrency, string source)
    {
        if (!string.Equals(rates.Base, baseCurrency, StringComparison.OrdinalIgnoreCase))
            throw new WrongBaseException(
                MessageText.Format(PricingErrorMessage.RatesWrongBase, new { Source = source, rates.Base, Currency = baseCurrency }));
    }
}
