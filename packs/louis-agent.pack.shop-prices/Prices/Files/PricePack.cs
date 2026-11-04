using System.Text.Json.Serialization;
using LouisAgent.Choices;
using LouisAgent.Packs.ShopPrices.Prices.Regions;

namespace LouisAgent.Packs.ShopPrices.Prices.Files;

/// <summary>
/// One pack of a product and its average price, nationally and per province: Stats SA's size as it writes it
/// ("2,5 Kilogram"), and the same size as a number we can scale by (2500 g). A province Stats SA has no price for
/// is simply missing.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public sealed record PricePack(
    [property: JsonPropertyName("size")] string Size,
    [property: JsonPropertyName("amount")] decimal? Amount,
    [property: JsonPropertyName("unit")] PackUnit Unit,
    [property: JsonPropertyName("prices")] IReadOnlyDictionary<string, decimal> Prices)
{
    /// <summary>
    /// The price in the province, or the national one when the province has none, with the region it came from;
    /// nothing when neither is known.
    /// </summary>
    public RegionPrice? PriceIn(Region region)
    {
        if (Prices.TryGetValue(region.Wire(), out decimal price)) return new RegionPrice(price, region);
        return Prices.TryGetValue(Region.National.Wire(), out decimal national)
            ? new RegionPrice(national, Region.National)
            : null;
    }
}
