namespace LouisAgent.Core.Pricing;

using System.Text.Json;

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
