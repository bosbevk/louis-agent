using System.Text.Json.Serialization;

namespace LouisAgent.Packs.ShopPrices.Prices.StatsSa;

/// <summary>
/// One line of the names file: Stats SA's code for a product ("01111201"), the name we show for it ("White Rice"), and
/// the names a shopping list may call it by ("white rice", "rice"), which Stats SA's sheets don't have.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public sealed record StatsSaName(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("product")] string Product,
    [property: JsonPropertyName("names")] IReadOnlyList<string> Names);
