using LouisAgent.Composition.Messages;
using System.Globalization;
using LouisAgent.Choices;
using LouisAgent.Packs.ShopPrices.Messages;
using LouisAgent.Packs.ShopPrices.Prices.Files;
using LouisAgent.Packs.ShopPrices.Prices.Regions;

namespace LouisAgent.Packs.ShopPrices.Imports;

/// <summary>
/// What differs between a month as it is stored and the same month uploaded again: each price that changed, came or
/// went (by product, pack and region), and each product whose names changed. None means the month is unchanged; any
/// means Stats SA corrected it (or the names file did), and the preview lists them before Save writes the month.
/// </summary>
public static class MonthChanges
{
    /// <summary>The differences, each one line, in the uploaded month's order and then what went.</summary>
    public static IReadOnlyList<string> Between(PriceFile stored, PriceFile uploaded)
    {
        var before = Prices(stored);
        var after = Prices(uploaded);
        var changes = new List<string>();
        foreach (var (key, price) in after)
        {
            if (!before.TryGetValue(key, out decimal old))
                changes.Add(Line(ShopPriceImportMessage.PriceAdded, key, null, price));
            else if (old != price) changes.Add(Line(ShopPriceImportMessage.PriceChanged, key, old, price));
        }

        changes.AddRange(before.Where(price => !after.ContainsKey(price.Key))
            .Select(price => Line(ShopPriceImportMessage.PriceRemoved, price.Key, price.Value, null)));
        changes.AddRange(Renamed(stored, uploaded)
            .Select(product => MessageText.Format(ShopPriceImportMessage.NamesChanged, new { Product = product })));
        return changes;
    }

    /// <summary>Every price in a month, by product, pack and region, in the month's order.</summary>
    private static Dictionary<PriceKey, decimal> Prices(PriceFile month)
    {
        var prices = new Dictionary<PriceKey, decimal>();
        foreach (var product in month.Products)
            foreach (var pack in product.Packs)
                foreach (var (region, price) in pack.Prices)
                    prices.TryAdd(new PriceKey(product.Product, pack.Size, region), price);
        return prices;
    }

    /// <summary>The products in both months whose names differ.</summary>
    private static IEnumerable<string> Renamed(PriceFile stored, PriceFile uploaded)
    {
        var before = stored.Products.DistinctBy(p => p.Product).ToDictionary(p => p.Product, p => p.Names);
        return uploaded.Products.Where(p => before.TryGetValue(p.Product, out var names) && !names.SequenceEqual(p.Names))
            .Select(p => p.Product).Distinct();
    }

    /// <summary>One change as a line, the prices in rand.</summary>
    private static string Line(ShopPriceImportMessage message, PriceKey key, decimal? old, decimal? price) =>
        MessageText.Format(message, new
        {
            key.Product,
            key.Size,
            Region = RegionName(key.Region),
            Old = old?.ToString("0.00", CultureInfo.InvariantCulture),
            New = price?.ToString("0.00", CultureInfo.InvariantCulture),
        });

    /// <summary>"the Western Cape" for "western-cape"; a key we don't know as it is.</summary>
    private static string RegionName(string wire) =>
        SettingChoice.TryParse(wire, out Region? region) && region is { } known ? MessageText.Template(known) : wire;

    /// <summary>Where a price is: the product, the pack's size and the region's wire name.</summary>
    private sealed record PriceKey(string Product, string Size, string Region);
}
