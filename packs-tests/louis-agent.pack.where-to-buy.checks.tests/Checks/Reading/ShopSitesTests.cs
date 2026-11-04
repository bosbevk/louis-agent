using LouisAgent.Packs.WhereToBuy.Checks.Reading;

namespace LouisAgent.Packs.WhereToBuy.Tests.Checks.Reading;

/// <summary>
/// The shops a list has a column for. No shop's prices are read (ADR 0069), so every such column breaks the rule.
/// </summary>
public class ShopSitesTests
{
    /// <summary>The top of a list with one Checkers column.</summary>
    private const string OneShop = "| Ingredient | For the recipe | To buy | Checkers |\n|---|---|---|---|\n";

    /// <summary>Every shop's column is listed by its site, each once, in the order they first appear.</summary>
    [Test]
    public void EveryShopsColumn_IsListedBySite()
    {
        string answer =
            "| Ingredient | For the recipe | To buy | Checkers | Pick n Pay | Shoprite | Best price |\n"
            + "|---|---|---|---|---|---|---|\n"
            + "| Onions | 2 | Onions 1 kg | [R34,99](https://www.checkers.co.za/p/3) | - | - | R34,99 Checkers |\n";

        Assert.That(ShopSites.UntooledShops(answer), Is.EqualTo(new[] { "checkers.co.za", "pnp.co.za", "shoprite.co.za" }));
    }

    /// <summary>A list with no shop column, the estimates list, lists nothing.</summary>
    [Test]
    public void AListWithNoShopColumn_ListsNothing() =>
        Assert.That(ShopSites.UntooledShops(OneShop.Replace("Checkers", "Estimated price") + "| Onions | 2 | Onions 1 kg | ± R25 |\n"),
            Is.Empty);
}
