namespace LouisAgent.Core.Tools.Shops;

/// <summary>
/// A search's products, and the area whose store answered it (F34): the user's town when the shop serves it from a store
/// of its own, null for the shop's default store (no place shared, or none served there).
/// </summary>
public sealed record ShopSearch(IReadOnlyList<ShopProduct> Products, string? Area);
