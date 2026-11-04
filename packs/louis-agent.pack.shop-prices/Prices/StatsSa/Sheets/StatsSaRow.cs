using LouisAgent.Packs.ShopPrices.Prices.Regions;

namespace LouisAgent.Packs.ShopPrices.Prices.StatsSa.Sheets;

/// <summary>
/// One price in Stats SA's spreadsheets: the product's code ("01111201") and its name and pack size as Stats SA wrote
/// them ("White Rice", "1 Kilogram"), where (the country or a province), the month, and the average price in rand.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values</seealso>
public sealed record StatsSaRow(string Code, string Product, string Size, Region Region, DateOnly Month, decimal Price);
