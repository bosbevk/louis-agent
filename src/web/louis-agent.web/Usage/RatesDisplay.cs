namespace LouisAgent.Web.Usage;

/// <summary>The API's display rates (F14): the server's display currency and a symbol per currency.</summary>
public sealed record RatesDisplay(
    [property: System.Text.Json.Serialization.JsonPropertyName("display_currency")] string DisplayCurrency,
    [property: System.Text.Json.Serialization.JsonPropertyName("display_symbol")] string DisplaySymbol,
    string Base,
    Dictionary<string, string> Symbols);
