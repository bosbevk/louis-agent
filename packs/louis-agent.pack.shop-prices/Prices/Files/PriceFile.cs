using System.Text.Json.Serialization;

namespace LouisAgent.Packs.ShopPrices.Prices.Files;

/// <summary>
/// One price table as the pack keeps it: who the prices are from, the month they are for, and the products.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public sealed record PriceFile(
    [property: JsonPropertyName("source")] string Source,
    [property: JsonPropertyName("as_of")] DateOnly AsOf,
    [property: JsonPropertyName("products")] IReadOnlyList<PriceProduct> Products);
