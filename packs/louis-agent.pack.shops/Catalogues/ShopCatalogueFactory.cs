
using LouisAgent.Packs.Shops.Places;
using LouisAgent.Packs.Shops.Providers;
using LouisAgent.Core.Tools.Web.Browsing;
using LouisAgent.Choices;

namespace LouisAgent.Packs.Shops.Catalogues;
/// <summary>
/// A catalogue per shop a deployment enables (F33), each reading its shop through the guarded browser. Which shops are
/// enabled is configuration (<c>SHOP_CATALOGUES</c>, step 3): none enabled is none created, so a deployment that hasn't
/// checked a shop's terms doesn't read it (ADR 0023).
/// </summary>
public static class ShopCatalogueFactory
{
    /// <summary>The shops there are catalogues for, from <see cref="ShopKind"/>.</summary>
    public static IReadOnlyList<string> SupportedShops { get; } = SettingChoice.Names<ShopKind>();

    /// <summary>
    /// The shops <c>SHOP_CATALOGUES</c> enables (comma-separated, case ignored, each once); a name that isn't a shop here
    /// enables nothing, and none at all leaves the shops toolset unavailable.
    /// </summary>
    public static IReadOnlyList<ShopKind> Enabled(string? setting) =>
        (setting ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(name => SettingChoice.TryParse(name, out ShopKind? shop) ? shop : null)
        .OfType<ShopKind>()
        .Distinct()
        .ToList();

    // F34: one throttle per shop for the process, so every engine's searches count against the same limit.
    private static readonly ShopThrottle CheckersThrottle = new();

    /// <param name="stores">The process's store cache (F34), shared by every engine's catalogues; a new one when null.</param>
    public static IReadOnlyDictionary<ShopKind, IShopCatalogue> Create(IReadOnlyList<ShopKind> enabled,
        IWebBrowser browser,
        StoreSessionCache? stores = null) =>
        enabled.Distinct().ToDictionary(shop => shop, shop => shop switch
        {
            // F34: Checkers answers for the store that serves the user's town, found once per town.
            ShopKind.Checkers => (IShopCatalogue)new CheckersCatalogue(browser,
                new CheckersStore(browser, PlaceCoordinates.OfAsync, stores, CheckersThrottle), CheckersThrottle),
            _ => throw new ArgumentOutOfRangeException(nameof(enabled), shop, null),
        });
}
