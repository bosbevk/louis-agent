
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Pricing.Messages;
using LouisAgent.Core.Pricing.Rates.Mappers;

namespace LouisAgent.Core.Pricing.Rates;
/// <summary>
/// Exchange rates for showing costs in another currency (F14), in louis-agent's own <c>rates.json</c> format, the only
/// one read: <c>{"as_of", "base", "source", "rates": {"ZAR": 16.5331, …}}</c>, each rate the price of one
/// <see cref="Base"/> in that currency. A provider's reply is turned into it by an <see cref="IRatesMapper"/>; the
/// tracked <c>config/rates.json</c> and louis-agent.api's <c>GET /v1/rates</c> are in it already.
/// </summary>
/// <remarks>Costs are only converted for display: the ledger keeps what was charged, in the price table's currency.</remarks>
public sealed partial class ExchangeRates
{
    /// <summary>
    /// The format's JSON names (snake_case), comments and trailing commas allowed like the price table's; a property
    /// it doesn't know is an error, so a provider's reply can't be read as ours by accident.
    /// </summary>
    internal static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    /// <summary>The rates' date (the provider's, e.g. the ECB's working day); shown with every converted figure.</summary>
    [JsonRequired]
    public string AsOf { get; init; } = "";

    /// <summary>The currency the rates are from; must be the price table's currency.</summary>
    [JsonRequired]
    public string Base { get; init; } = "";

    /// <summary>Where the rates came from, e.g. "frankfurter (ECB reference rates)".</summary>
    public string? Source { get; init; }

    /// <summary>Currency code → how much of it one <see cref="Base"/> buys.</summary>
    [JsonRequired]
    public Dictionary<string, decimal> Rates { get; init; } = [];

    /// <summary>No rates: costs are shown in the price table's currency.</summary>
    public static ExchangeRates Empty { get; } = new();

    [JsonIgnore] public bool IsEmpty => Rates.Count == 0;

    /// <summary>The rate from <see cref="Base"/> to <paramref name="currency"/>: 1 for the base itself, null when unknown.</summary>
    public decimal? RateFor(string currency) =>
        string.Equals(currency, Base, StringComparison.OrdinalIgnoreCase) ? 1m
        : Rates.TryGetValue(currency.ToUpperInvariant(), out decimal rate) ? rate
        : null;

    /// <summary>
    /// Loads the rates at <paramref name="path"/>; a missing file gives <see cref="Empty"/>. A malformed file throws
    /// <see cref="InvalidDataException"/> naming the file, like the price table.
    /// </summary>
    public static ExchangeRates Load(string path) => File.Exists(path) ? Parse(File.ReadAllText(path), path) : Empty;

    /// <summary>
    /// Fetches rates from <paramref name="url"/> and turns the reply into ours with <paramref name="mapper"/>. Throws
    /// <see cref="HttpRequestException"/> when it can't be fetched; a reply the mapper can't read, or that isn't valid
    /// once mapped, throws <see cref="InvalidDataException"/>.
    /// </summary>
    public static async Task<ExchangeRates> LoadAsync(HttpClient http, Uri url, IRatesMapper mapper,
        CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await http.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        string body = await response.Content.ReadAsStringAsync(cancellationToken);
        return Parse(mapper.ToRatesJson(body, url.ToString()), url.ToString());
    }

    /// <summary>Rates in our format (<paramref name="json"/>), checked the same way wherever they came from.</summary>
    internal static ExchangeRates Parse(string json, string source)
    {
        ExchangeRates? rates;
        try
        {
            rates = JsonSerializer.Deserialize<ExchangeRates>(json, Json);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException(
                MessageText.Format(PricingErrorMessage.RatesNotRatesJson, new { Source = source, Reason = ex.Message }),
                ex);
        }

        if (rates is null)
            throw new InvalidDataException(MessageText.Format(PricingErrorMessage.RatesEmpty, new { Source = source }));
        if (!CurrencyCode().IsMatch(rates.Base))
            throw new InvalidDataException(MessageText.Format(PricingErrorMessage.RatesBaseNotACurrency,
                new { Source = source, rates.Base }));
        foreach (var (code, rate) in rates.Rates)
        {
            if (!CurrencyCode().IsMatch(code))
                throw new InvalidDataException(MessageText.Format(PricingErrorMessage.RatesCodeNotACurrency,
                    new { Source = source, Code = code }));
            if (rate <= 0)
                throw new InvalidDataException(MessageText.Format(PricingErrorMessage.RateNotPositive,
                    new { Source = source, Code = code }));
        }

        return rates;
    }

    /// <summary>Our format as JSON, for <c>GET /v1/rates</c> and the mappers.</summary>
    public string ToJson() => JsonSerializer.Serialize(this, Json);

    /// <summary>An ISO 4217 code: three capital letters.</summary>
    [GeneratedRegex("^[A-Z]{3}$")]
    private static partial Regex CurrencyCode();
}
