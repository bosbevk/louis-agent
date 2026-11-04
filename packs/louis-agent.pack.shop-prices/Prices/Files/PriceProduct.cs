using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace LouisAgent.Packs.ShopPrices.Prices.Files;

/// <summary>
/// A product as the table prices it ("Beef mince"), the names a shopping list may call it by ("mince",
/// "maalvleis"), and the packs it is priced in. The same names in the shipped file and in MongoDB.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
public sealed record PriceProduct(
    [property: JsonPropertyName("product")][property: BsonElement("product")] string Product,
    [property: JsonPropertyName("names")][property: BsonElement("names")] IReadOnlyList<string> Names,
    [property: JsonPropertyName("packs")][property: BsonElement("packs")] IReadOnlyList<PricePack> Packs);
