namespace LouisAgent.Web.Shared.Usage;

/// <summary>The API's <c>GET /rates</c> (F14), in louis-agent's rates.json format: one <see cref="Base"/> in each currency.</summary>
public sealed record RatesTable(
    [property: System.Text.Json.Serialization.JsonPropertyName("as_of")] string AsOf,
    string Base,
    Dictionary<string, decimal> Rates);
