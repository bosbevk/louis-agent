namespace louis_agent.core.usage;

using System.Globalization;
using System.Text.Json;

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

/// <summary>Our own format, passed through: for a host reading another louis-agent's <c>GET /rates</c>.</summary>
public sealed class RatesJsonMapper : IRatesMapper
{
    public const string FormatName = "rates-json";

    public string Format => FormatName;

    public string ToRatesJson(string body, string source) => body;
}

/// <summary>
/// <a href="https://frankfurter.dev">Frankfurter</a>'s reply (the ECB's daily reference rates, free, no key):
/// <c>{"amount":1.0,"base":"USD","date":"2026-10-09","rates":{"ZAR":16.5331,…}}</c>. <c>date</c> becomes <c>as_of</c>,
/// and the rates are divided by <c>amount</c>, so they are always per one unit of the base.
/// </summary>
public sealed class FrankfurterRatesMapper : IRatesMapper
{
    public const string FormatName = "frankfurter";

    public string Format => FormatName;

    public string ToRatesJson(string body, string source)
    {
        try
        {
            using JsonDocument reply = JsonDocument.Parse(body);
            JsonElement root = reply.RootElement;
            decimal amount = root.TryGetProperty("amount", out JsonElement a) ? a.GetDecimal() : 1m;
            if (amount <= 0) throw new InvalidDataException($"Frankfurter rates {source}: amount must be positive.");

            var rates = new ExchangeRates
            {
                AsOf = root.GetProperty("date").GetString() ?? "",
                Base = root.GetProperty("base").GetString() ?? "",
                Source = "frankfurter (ECB reference rates)",
                Rates = root.GetProperty("rates").EnumerateObject()
                    .ToDictionary(r => r.Name, r => decimal.Round(r.Value.GetDecimal() / amount, 6, MidpointRounding.AwayFromZero)),
            };
            return rates.ToJson();
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
        {
            throw new InvalidDataException($"Frankfurter rates {source} are not in Frankfurter's format: {ex.Message}", ex);
        }
    }
}

/// <summary>The mapper for a <c>RATES_FORMAT</c> value.</summary>
public static class RatesMappers
{
    public static IRatesMapper For(string? format) => (format ?? FrankfurterRatesMapper.FormatName).ToLower(CultureInfo.InvariantCulture) switch
    {
        FrankfurterRatesMapper.FormatName => new FrankfurterRatesMapper(),
        RatesJsonMapper.FormatName => new RatesJsonMapper(),
        _ => throw new InvalidDataException(
            $"RATES_FORMAT must be '{FrankfurterRatesMapper.FormatName}' or '{RatesJsonMapper.FormatName}', got '{format}'."),
    };
}