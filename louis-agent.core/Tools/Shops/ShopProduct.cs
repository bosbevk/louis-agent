namespace LouisAgent.Core.Tools.Shops;

/// <summary>
/// A product as the shop sells it: what it costs now (<see cref="Price"/>, a special's price when there is one), per
/// <see cref="UnitOfMeasure"/> ("kg" or "each"), the price per kg for what's sold by weight, its size when the shop gives
/// one apart from the name, and the page that shows it.
/// </summary>
public sealed record ShopProduct(string Name, decimal Price, string Currency, string? Size, decimal? UnitPrice, string UnitOfMeasure, Uri Url);
