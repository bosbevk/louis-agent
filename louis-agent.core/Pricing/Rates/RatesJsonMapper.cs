namespace LouisAgent.Core.Pricing.Rates;

/// <summary>Our own format, passed through: for a host reading another louis-agent's <c>GET /rates</c>.</summary>
public sealed class RatesJsonMapper : IRatesMapper
{
    public const string FormatName = "rates-json";

    public string Format => FormatName;

    public string ToRatesJson(string body, string source) => body;
}
