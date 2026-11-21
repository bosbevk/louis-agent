namespace LouisAgent.Web.Usage;

/// <summary>The API's rates (F14), in the rates.json format: one <see cref="Base"/> in each currency.</summary>
public sealed record RatesTable(
    [property: System.Text.Json.Serialization.JsonPropertyName("as_of")] string AsOf,
    string Base,
    Dictionary<string, decimal> Rates);
