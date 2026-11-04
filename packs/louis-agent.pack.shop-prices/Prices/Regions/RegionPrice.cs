namespace LouisAgent.Packs.ShopPrices.Prices.Regions;

/// <summary>A pack's average price, and the region it is the average of (the user's province, or the country).</summary>
public sealed record RegionPrice(decimal Price, Region Region);
