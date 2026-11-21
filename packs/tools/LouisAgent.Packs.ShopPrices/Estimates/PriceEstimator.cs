using LouisAgent.Packs.ShopPrices.Prices;
using LouisAgent.Packs.ShopPrices.Prices.Regions;

namespace LouisAgent.Packs.ShopPrices.Estimates;

/// <summary>
/// Prices one shopping-list line from the tables: the same pack size when the table has it, else scaled from the
/// nearest size it prices, times the number of packs, rounded to whole rand. The model never does this sum.
/// </summary>
/// <remarks>
/// Guard clauses, one per case it can't price, the estimate last (ADR 0051). "Nearest" is by ratio, not by grams, so
/// 500 g sits as near to 250 g as to 1 kg, and on a tie the table's first pack wins (Stats SA's price per kilogram
/// comes before its 1 kg pack).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0051-flat-conditionals-and-named-endpoints.md">ADR 0051: no nested conditionals</seealso>
public static class PriceEstimator
{
    /// <summary>The line's estimate in the region, from the tables.</summary>
    public static PriceEstimate Estimate(BasketItem item, PriceBook table, Region region)
    {
        if (table.Find(item.Words) is not { } match) return new PriceEstimate(item, EstimateKind.NotFound);
        var priced = match.Product.Packs
            .Select(pack => (Pack: pack, Price: pack.PriceIn(region)))
            .Where(p => p.Price is not null)
            .ToList();
        if (priced.Count == 0) return new PriceEstimate(item, EstimateKind.NotFound, match);

        // No size given: one of the table's own packs, the first it prices.
        if (item.Amount is not { } amount)
        {
            var (pack, price) = priced[0];
            return new PriceEstimate(item, EstimateKind.TablePack, match, pack, price, Whole(price!.Price * item.Packs));
        }

        var sameUnit = priced.Where(p => p.Pack.Unit == item.Unit && p.Pack.Amount > 0).ToList();
        if (sameUnit.Count == 0 || amount <= 0)
            return new PriceEstimate(item, EstimateKind.OtherUnit, match);

        // The same size when the table prices it; else the nearest size, scaled to the line's.
        var same = sameUnit.FirstOrDefault(p => p.Pack.Amount == amount);
        if (same.Pack is not null)
            return new PriceEstimate(item, EstimateKind.SamePack, match, same.Pack, same.Price,
                Whole(same.Price!.Price * item.Packs));

        var nearest = sameUnit.MinBy(p => Math.Abs(Math.Log((double)(p.Pack.Amount!.Value / amount))));
        decimal scaled = nearest.Price!.Price / nearest.Pack.Amount!.Value * amount * item.Packs;
        return new PriceEstimate(item, EstimateKind.Scaled, match, nearest.Pack, nearest.Price, Whole(scaled));
    }

    /// <summary>An amount in whole rand, half a rand rounded up.</summary>
    private static decimal Whole(decimal rand) => Math.Round(rand, 0, MidpointRounding.AwayFromZero);
}
