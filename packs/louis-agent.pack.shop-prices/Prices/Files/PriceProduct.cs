using System.Text.Json.Serialization;

namespace LouisAgent.Packs.ShopPrices.Prices.Files;

/// <summary>
/// A product as the table prices it ("Beef mince"), the names a shopping list may call it by ("mince",
/// "maalvleis"), and the packs it is priced in.
/// </summary>
public sealed record PriceProduct(
    [property: JsonPropertyName("product")] string Product,
    [property: JsonPropertyName("names")] IReadOnlyList<string> Names,
    [property: JsonPropertyName("packs")] IReadOnlyList<PricePack> Packs);
