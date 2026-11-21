namespace LouisAgent.Packs.ShopPrices.Tests;

/// <summary>The settings say where the price store is; unset, the pack prices from its own tables.</summary>
public class ShopPricesOptionsTests
{
    /// <summary>Nothing set: no store, and the default database.</summary>
    [Test]
    public void Unset_IsNoStore_AndTheDefaultDatabase()
    {
        var options = ShopPricesOptions.FromEnvironment(_ => null);

        Assert.That((options.MongoUrl, options.MongoDatabase), Is.EqualTo(((string?)null, "shop_prices")));
    }

    /// <summary>Both set, trimmed; a blank value counts as unset.</summary>
    [Test]
    public void Set_IsReadTrimmed_AndABlankIsUnset()
    {
        var options = ShopPricesOptions.FromEnvironment(name => name switch
        {
            "SHOP_PRICES_MONGO_URL" => " mongodb://mongo:27017 ",
            "SHOP_PRICES_MONGO_DATABASE" => " prices ",
            _ => null,
        });
        var blank = ShopPricesOptions.FromEnvironment(_ => "  ");

        Assert.That((options.MongoUrl, options.MongoDatabase), Is.EqualTo(("mongodb://mongo:27017", "prices")));
        Assert.That(blank.MongoUrl, Is.Null);
    }
}
