namespace LouisAgent.Core.Pricing.Rates;



/// <summary>
/// Turns an exchange-rate provider's reply into louis-agent's own <c>rates.json</c> format (<see cref="ExchangeRates"/>),
/// the only one the code reads. One mapper per provider format, chosen by <c>RATES_FORMAT</c>, like the
/// <see cref="IUsageMapper"/> per LLM provider: a new provider is one new mapper and nothing else changes.
/// </summary>
public interface IRatesMapper
{
    /// <summary>The <c>RATES_FORMAT</c> value that selects this mapper.</summary>
    string Format { get; }

    /// <summary>
    /// The reply in our format. Throws <see cref="InvalidDataException"/>, naming <paramref name="source"/>, when the reply
    /// isn't in the provider's format.
    /// </summary>
    string ToRatesJson(string body, string source);
}
