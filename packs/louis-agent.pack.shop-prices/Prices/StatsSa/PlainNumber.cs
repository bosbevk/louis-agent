namespace LouisAgent.Packs.ShopPrices.Prices.StatsSa;

/// <summary>
/// A decimal without the trailing zeros its sum or its text gave it: 2.5 kg times 1000 is 2500.0 and Stats SA's
/// "27.50" is 27.50, but the price tables, the feed and MongoDB show 2500 and 27.5, as the shipped file does.
/// </summary>
public static class PlainNumber
{
    /// <summary>The same value at its smallest scale (dividing by one with 28 zeros drops trailing zeros).</summary>
    public static decimal Of(decimal value) => value / 1.0000000000000000000000000000m;
}
