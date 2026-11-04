namespace LouisAgent.Web.Shared.Usage;

/// <summary>The API's <c>GET /v1/rates/display</c> (F14): the server's display currency (the PC's region) and a symbol per currency.</summary>
public sealed record RatesDisplay(
    [property: System.Text.Json.Serialization.JsonPropertyName("display_currency")] string DisplayCurrency,
    [property: System.Text.Json.Serialization.JsonPropertyName("display_symbol")] string DisplaySymbol,
    string Base,
    Dictionary<string, string> Symbols);
