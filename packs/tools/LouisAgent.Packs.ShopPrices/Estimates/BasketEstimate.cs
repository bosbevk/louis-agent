using LouisAgent.Packs.ShopPrices.Prices;
using LouisAgent.Packs.ShopPrices.Prices.Regions;

namespace LouisAgent.Packs.ShopPrices.Estimates;

/// <summary>
/// A whole list's estimate: every line's, the total of those we could price, and the price per person when the list
/// says how many it feeds. The total adds the whole-rand estimates as shown, so the list adds up to the rand.
/// </summary>
public sealed record BasketEstimate(Region Region, IReadOnlyList<PriceEstimate> Lines, int? Serves)
{
    /// <summary>The lines' estimates added up; the lines we couldn't price are left out, and said so.</summary>
    public decimal Total => Lines.Sum(line => line.Rand ?? 0);

    /// <summary>The lines we couldn't price.</summary>
    public IReadOnlyList<PriceEstimate> Unpriced => [.. Lines.Where(line => line.Rand is null)];

    /// <summary>The total shared by the people it feeds, in whole rand; nothing without a number of people.</summary>
    public decimal? PerPerson => Serves is > 0
        ? Math.Round(Total / Serves.Value, 0, MidpointRounding.AwayFromZero)
        : null;

    /// <summary>Prices every line of the list in the region.</summary>
    public static BasketEstimate Of(IEnumerable<string> lines, PriceBook table, Region region, int? serves) =>
        new(region, [.. lines.Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => PriceEstimator.Estimate(BasketItem.Parse(line), table, region))], serves);
}
