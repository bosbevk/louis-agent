using LouisAgent.Packs.ShopPrices.Prices;
using LouisAgent.Packs.ShopPrices.Prices.Regions;
using LouisAgent.Packs.ShopPrices.Prices.Files;

namespace LouisAgent.Packs.ShopPrices.Estimates;

/// <summary>
/// One line's estimate: the line, the product and pack it was priced from with that pack's average price, and the
/// estimate in whole rand. The estimate is null when we couldn't make one: unknown is never R0 (ADR 0007).
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0007-measure-every-request.md">ADR 0007: unknown is null, never 0</seealso>
public sealed record PriceEstimate(
    BasketItem Item,
    EstimateKind Kind,
    PriceMatch? Match = null,
    PricePack? Pack = null,
    RegionPrice? Price = null,
    decimal? Rand = null);
