namespace LouisAgent.Packs.Shops.Catalogues;

/// <summary>
/// One shop's products, searched in its own catalogue (F33): the shop's own prices, exact and linked, read in code rather
/// than by a model from a page. A catalogue reads the shop's site through the guarded browser, so its address rule holds.
/// Problems the cook can act on throw <see cref="ShopCatalogueException"/>.
/// </summary>
public interface IShopCatalogue
{
    ShopKind Shop { get; }

    /// <param name="place">Where the user is (F34): a shop that serves by area searches the store serving it; null: the default store.</param>
    Task<ShopSearch> SearchAsync(string query, int maxResults, string? place = null, CancellationToken cancellationToken = default);
}
