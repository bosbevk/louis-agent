using LouisAgent.Packs.Shops.Checks;

namespace LouisAgent.Packs.Shops.Tests.Checks;

/// <summary>
/// The shops a list has a column for that the cook can't read. Checkers has its shop tools, and PnP and Woolworths a
/// rendered page; Shoprite has neither, so its column breaks the rule.
/// </summary>
public class ShopSitesTests
{
    /// <summary>The top of a list with one Checkers column.</summary>
    private const string OneShop = "| Ingredient | For the recipe | To buy | Checkers |\n|---|---|---|---|\n";

    /// <summary>A column for a shop the cook has no tool for is listed by its site; ones it can read aren't.</summary>
    [Test]
    public void AShopWithoutATool_IsListedBySite()
    {
        string answer =
            "| Ingredient | For the recipe | To buy | Checkers | Pick n Pay | Shoprite | Best price |\n"
            + "|---|---|---|---|---|---|---|\n"
            + "| Onions | 2 | Onions 1 kg | [R34,99](https://www.checkers.co.za/p/3) | - | - | R34,99 Checkers |\n";

        Assert.That(ShopSites.UntooledShops(answer), Is.EqualTo(new[] { "shoprite.co.za" }));
    }

    /// <summary>Columns only for shops the cook can read list nothing.</summary>
    [Test]
    public void ShopsTheCookCanRead_AreNotListed()
    {
        Assert.That(
            ShopSites.UntooledShops(OneShop.Replace("Checkers", "Woolworths") + "| Onions | 2 | Onions 1 kg | - |\n"),
            Is.Empty);
        Assert.That(ShopSites.UntooledShops(OneShop + "| Onions | 2 | Onions 1 kg | - |\n"), Is.Empty);
    }
}
