using LouisAgent.Choices;
using LouisAgent.Packs.ShopPrices.Prices.Files;
using LouisAgent.Packs.ShopPrices.Prices.StatsSa.Sheets;

namespace LouisAgent.Packs.ShopPrices.Prices.StatsSa;

/// <summary>
/// Turns Stats SA's rows into the pack's price tables, one per month, oldest first: each product named from the names
/// file, each pack's size read into an amount we can scale by, and its prices by region (the country first, then the
/// provinces, in the sheets' order). A month holds only the packs Stats SA priced that month.
/// </summary>
/// <remarks>
/// A Data Mapper between two shapes (Fowler, PoEAA): Stats SA's wide sheets on one side, the pack's
/// <see cref="PriceFile"/> on the other, so nothing after it knows the sheets. The August 2026 release gives back the
/// table the pack ships for that month (its test).
/// </remarks>
public static class StatsSaPrices
{
    /// <summary>Who the prices are from, as every Stats SA table names it.</summary>
    public const string Source = "Stats SA, CPI average prices (P0141), all urban areas and provinces";

    /// <summary>A table for each month the rows hold, oldest first.</summary>
    public static IReadOnlyList<PriceFile> ToFiles(IReadOnlyList<StatsSaRow> rows, StatsSaNames names) =>
    [
        .. rows.GroupBy(row => row.Month).OrderBy(month => month.Key)
            .Select(month => new PriceFile(Source, month.Key, [.. month.GroupBy(row => row.Code).Select(p => Product(p, names))]))
    ];

    /// <summary>One product's packs in the order the sheets list them, named from the names file.</summary>
    private static PriceProduct Product(IGrouping<string, StatsSaRow> rows, StatsSaNames names)
    {
        var named = names.For(rows.First());
        return new PriceProduct(named.Product, named.Names, [.. rows.GroupBy(row => row.Size).Select(Pack)]);
    }

    /// <summary>One pack: its size as Stats SA wrote it and as an amount, and its price in each region.</summary>
    private static PricePack Pack(IGrouping<string, StatsSaRow> rows)
    {
        var size = PackSize.Read(rows.Key);
        // A region priced twice for one pack keeps the first price the sheets give.
        var prices = rows.DistinctBy(row => row.Region).ToDictionary(row => row.Region.Wire(), row => row.Price);
        return new PricePack(rows.Key, size.Amount, size.Unit, prices);
    }
}
