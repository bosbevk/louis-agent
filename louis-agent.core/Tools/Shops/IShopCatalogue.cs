namespace LouisAgent.Core.Tools.Shops;

/// <summary>
/// One shop's products, searched in its own catalogue (F33): the shop's own prices, exact and linked, read in code rather
/// than by a model from a page. A catalogue reads the shop's site through the guarded browser, so its address rule holds.
/// Problems the cook can act on throw <see cref="ShopCatalogueException"/>.
/// </summary>
public interface IShopCatalogue
{
    ShopKind Shop { get; }

    Task<IReadOnlyList<ShopProduct>> SearchAsync(string query, int maxResults, CancellationToken cancellationToken = default);
}
