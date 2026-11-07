namespace LouisAgent.Packs.ShopPrices;

/// <summary>
/// The shop-prices pack's own settings, read once by the composition root: where the price store is. Unset, the pack
/// prices from the tables it ships, in memory.
/// </summary>
/// <remarks>
/// A plain options object with a reader that takes the variable lookup, as <c>RecipesOptions</c> is, so a test needs
/// no real environment.
/// </remarks>
public sealed class ShopPricesOptions
{
    /// <summary>The database the prices are kept in when none is named.</summary>
    public const string DefaultDatabase = "shop_prices";

    /// <summary>
    /// SHOP_PRICES_MONGO_URL: the MongoDB the price tables are kept in, one document per source and month; unset, the
    /// shipped tables in memory. A secret, so it lives in <c>config/.env.secrets</c>.
    /// </summary>
    public string? MongoUrl { get; init; }

    /// <summary>SHOP_PRICES_MONGO_DATABASE: the database on that server; unset, <c>shop_prices</c>.</summary>
    public string MongoDatabase { get; init; } = DefaultDatabase;

    /// <summary>Reads the settings; the variable lookup is replaceable for tests.</summary>
    public static ShopPricesOptions FromEnvironment(Func<string, string?>? getVariable = null)
    {
        getVariable ??= Environment.GetEnvironmentVariable;
        string? Get(string name) => string.IsNullOrWhiteSpace(getVariable(name)) ? null : getVariable(name)!.Trim();
        return new ShopPricesOptions
        {
            MongoUrl = Get("SHOP_PRICES_MONGO_URL"),
            MongoDatabase = Get("SHOP_PRICES_MONGO_DATABASE") ?? DefaultDatabase,
        };
    }
}
