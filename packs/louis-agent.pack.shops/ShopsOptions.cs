namespace LouisAgent.Packs.Shops;

/// <summary>The shop pack's own settings (CL15), read once by the composition root; the names are F33's and F34's.</summary>
public sealed class ShopsOptions
{
    /// <summary>SHOP_CATALOGUES (F33): the shops whose catalogue may be read, comma-separated (checkers); unset, none.</summary>
    public string? ShopCatalogues { get; init; }

    /// <summary>SHOP_STORE_CACHE (F34): a file that keeps each town's store a day, across restarts; unset, memory only.</summary>
    public string? ShopStoreCache { get; init; }

    public static ShopsOptions FromEnvironment(Func<string, string?>? getVariable = null)
    {
        getVariable ??= Environment.GetEnvironmentVariable;
        string? Get(string name) => string.IsNullOrWhiteSpace(getVariable(name)) ? null : getVariable(name)!.Trim();
        return new ShopsOptions { ShopCatalogues = Get("SHOP_CATALOGUES"), ShopStoreCache = Get("SHOP_STORE_CACHE") };
    }
}
