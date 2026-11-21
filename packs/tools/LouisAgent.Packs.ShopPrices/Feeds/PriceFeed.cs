using System.Text.Json.Serialization;
using LouisAgent.Packs.ShopPrices.Prices.Files;

namespace LouisAgent.Packs.ShopPrices.Feeds;

/// <summary>
/// The shop-prices feed as it is sent: the notice that says what the numbers are and whose, and the tables, each in
/// the shape the pack ships it.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public sealed record PriceFeed(
    [property: JsonPropertyName("notice")] string Notice,
    [property: JsonPropertyName("tables")] IReadOnlyList<PriceFile> Tables);
