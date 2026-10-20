namespace LouisAgent.Packs.Shops.Catalogues;

/// <summary>
/// A search's products, and the area whose store answered it (F34): the user's town when the shop serves it from a store
/// of its own, null for the shop's default store (no place shared, or none served there). <see cref="Store"/> is the
/// store that answered, by the shop's own id; <see cref="AreaIgnored"/> says the town's store was asked for but another
/// answered (Louis: "how can we verify that the product comes from the malmesbury store?").
/// </summary>
public sealed record ShopSearch(IReadOnlyList<ShopProduct> Products, string? Area, string? Store = null, bool AreaIgnored = false);
