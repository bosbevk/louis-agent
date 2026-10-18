namespace LouisAgent.Core.Tools.Shops;

/// <summary>
/// A product as the shop sells it: what a pack costs now (<see cref="Price"/>, a special's price when there is one), per
/// <see cref="UnitOfMeasure"/> ("kg" or "each"), the price per kg for what's sold by weight, its size when the shop gives
/// one apart from the name, and the page that shows it. A pack sold by weight costs what it weighs: <see cref="Price"/>
/// is the lightest pack's and <see cref="PriceTo"/> the heaviest's; with no pack weight, both are null and only
/// <see cref="UnitPrice"/> is known, since a price per kg is never a pack's price (PR #58's review). <see cref="Image"/>
/// is the photo the shop's own page shows, when it has one; <see cref="Deal"/> its deal (F34), when it has one.
/// </summary>
public sealed record ShopProduct(string Name, decimal? Price, decimal? PriceTo, string Currency, string? Size, decimal? UnitPrice, string UnitOfMeasure, Uri Url, Uri? Image = null, ShopDeal? Deal = null);
