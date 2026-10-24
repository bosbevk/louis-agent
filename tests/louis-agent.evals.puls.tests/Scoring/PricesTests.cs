using LouisAgent.Evals.Puls.Scoring;

namespace LouisAgent.Evals.Puls.Tests.Scoring;

// The Python's test_scoring.py, the price rules (F31 step 7a; the cases and their numbers as the Python has them). Each
// expected value is the Python's, so the C# and the Python score an answer the same way.
public class PricesTests
{
    private const string HeaderEn = "| Ingredient | Recipe needs | Buy | Checkers | Shoprite | Pick n Pay | Best price |\n|---|---|---|---|---|---|---|\n";
    private const string HeaderAf = "| Bestanddeel | Hoeveel | Koop | Checkers | Shoprite | Goedkoopste |\n|---|---|---|---|---|---|\n";
    private const string Thighs = "https://www.checkers.co.za/product/farmers-choice-chicken-8-piece-thighs-per-kg-10732975KG";

    [Test]
    public void RandAmounts_AreCounted() =>
        Assert.That(Prices.RandAmounts("Meel R22.99, eiers R 45,00 en melk R18"), Is.EqualTo(3));

    [Test]
    public void APriceWithItsLink_IsPriced()
    {
        string answer = "| Bestanddeel | Checkers |\n|---|---|\n| Meel | [R22.99](https://www.checkers.co.za/p/1) |\n"
                        + "| Eiers | onbekend |\n| Melk | ±R18 |\n";

        Assert.That(Prices.Priced(answer), Is.EqualTo(new PriceCount(1, 3, 0, 0)));
    }

    [Test]
    public void ADash_IsAShopWithoutAPrice()
    {
        // The cook writes `-` for a price it couldn't find (Louis, review of PR #55). A row priced with its link at one shop
        // and `-` at another is priced; a row of dashes is listed, not priced, not guessed.
        string answer = "| Bestanddeel | Checkers | Shoprite | Beste prys |\n|---|---|---|---|\n"
                        + "| Suurlemoene | [R12,99](https://www.checkers.co.za/p/1) | - | R12,99 Checkers |\n"
                        + "| Koljander | - | - | - |\n";

        Assert.That(Prices.Priced(answer), Is.EqualTo(new PriceCount(1, 2, 0, 0)));
    }

    [Test]
    public void APriceWithoutALink_IsAGuessNotPriced()
    {
        string answer = "| Bestanddeel | Prys |\n|---|---|\n| Meel | R22.99 |\n| Eiers | R45.00 |\n| Melk | [R18](https://pnp.co.za/m) |\n";

        Assert.That(Prices.Priced(answer), Is.EqualTo(new PriceCount(1, 3, 2, 0)));
    }

    [Test]
    public void ARecipeLink_IsNoSource()
    {
        string answer = "| Bestanddeel | Checkers |\n|---|---|\n| Hoender | [R89,99](https://www.food24.com/recipes/hoender) |\n";

        Assert.That(Prices.Priced(answer), Is.EqualTo(new PriceCount(0, 1, 1, 0)));
    }

    [Test]
    public void AnotherShopsLink_IsNoSourceForThisShopsPrice()
    {
        string answer = "| Bestanddeel | Checkers | Pick n Pay |\n|---|---|---|\n"
                        + "| Hoender | [R89,99](https://www.pnp.co.za/hoender) | - |\n"
                        + "| Uie | - | [R24,99](https://www.pnp.co.za/uie) |\n";

        Assert.That(Prices.Priced(answer), Is.EqualTo(new PriceCount(1, 2, 1, 0)));
    }

    [Test]
    public void AReferenceStyleLink_IsItsUrl()
    {
        string answer = "| Bestanddeel | Checkers |\n|---|---|\n| Hoender | [R89,99][1] |\n| Uie | [R24,99][uie] |\n\n"
                        + "[1]: https://www.checkers.co.za/p/hoender\n[uie]: https://www.checkers.co.za/p/uie \"Uie\"\n";

        Assert.That(Prices.Priced(answer), Is.EqualTo(new PriceCount(2, 2, 0, 0)));
    }

    [Test]
    public void APriceWhoseProductLinksToTheShop_IsPriced()
    {
        // F34, run 26527259bf31: Puls alone linked the product, not the price, and scored 0 priced. The row's link to the
        // product at the price's shop is its source too.
        string answer = "| Item | Pryse | Subtotaal |\n|---|---|---|\n"
                        + "| [Onions 1kg](https://www.checkers.co.za/product/onions-1kg-10135699EA) | R34.99 | R34.99 |\n"
                        + "| [Tastic Long Grain Parboiled Rice 500g](https://www.checkers.co.za/product/tastic-long-grain-parboiled-white-rice-500g-10130260EA) | R19.99 | R19.99 |\n";

        Assert.That(Prices.Priced(answer), Is.EqualTo(new PriceCount(2, 2, 0, 0)));
    }

    [Test]
    public void OnePriceForAProductLinkedAsSoldByWeight_IsItsPricePerKg()
    {
        string answer = "| Item | Pryse |\n|---|---|\n"
                        + "| [Farmer's Choice Chicken 8 Piece Braai Pack Per Kg](https://www.checkers.co.za/product/farmers-choice-chicken-8-piece-braai-pack-per-kg-10732967KG), 1 kg | R59.99 |\n";

        Assert.That(Prices.Priced(answer), Is.EqualTo(new PriceCount(0, 1, 0, 1)));
    }

    [Test]
    public void AProductLinkedToAnotherShopOrARecipe_IsNoSource()
    {
        string answer = "| Item | Checkers |\n|---|---|\n| [Melk](https://www.pnp.co.za/melk) | R18,99 |\n"
                        + "| [Hoender](https://www.food24.com/recipes/hoender) | R89,99 |\n";

        Assert.That(Prices.Priced(answer), Is.EqualTo(new PriceCount(0, 2, 2, 0)));
    }

    [Test]
    public void AListHeadedWat_IsTheShoppingList()
    {
        // Cook check 42d837f13659: Puls alone headed its list `| Wat | Pak | Prys |`, and the scorer found no list.
        string answer = "| Wat | Pak | Prys |\n|---|---|---|\n"
                        + "| [Uie](https://www.checkers.co.za/product/onions-1kg-10135699EA) | 1 kg | R34.99 |\n";

        Assert.That(Prices.Priced(answer), Is.EqualTo(new PriceCount(1, 1, 0, 0)));
        Assert.That(Prices.Priced(answer.Replace("Wat", "What")), Is.EqualTo(new PriceCount(1, 1, 0, 0)));
        Assert.That(Prices.Priced("| Wat | Waarom |\n|---|---|\n| Rys | R19,99 nie gevind |\n"), Is.Null,
            "the cook's Onseker table is no list");
    }

    [Test]
    public void AShopsOwnSubdomain_IsTheShop()
    {
        string answer = "| Bestanddeel | Woolworths |\n|---|---|\n| Melk | [R32,99](https://shop.woolworths.co.za/melk) |\n";

        Assert.That(Prices.Priced(answer), Is.EqualTo(new PriceCount(1, 1, 0, 0)));
    }

    [Test]
    public void AHostThatOnlyEndsLikeAShop_IsNotTheShop()
    {
        string answer = "| Bestanddeel | Checkers |\n|---|---|\n| Melk | [R32,99](https://notcheckers.co.za/melk) |\n";

        Assert.That(Prices.Priced(answer), Is.EqualTo(new PriceCount(0, 1, 1, 0)));
    }

    [Test]
    public void TheCooksOwnTally_IsAClaimNotARating()
    {
        // "Geprys: 11 van 11" under a list priced from memory: the rows say what was sourced.
        string answer = "| Bestanddeel | Prys |\n|---|---|\n| Meel | R22.99 |\n\nGeprys: 11 van 11 bestanddele";

        Assert.That(Prices.Priced(answer), Is.EqualTo(new PriceCount(0, 1, 1, 0)));
    }

    [Test]
    public void OnlyTheShoppingListsIngredientRows_Count()
    {
        // The cook's real answer (F31 step 11b): the list with its bold totals, then the branches and what's unsure.
        string answer = "| Bestanddeel | Koop | Checkers |\n|---|---|---|\n| Boerewors | 1 kg | unknown |\n"
                        + "| Uie | 1 kg | [R24.99](https://www.checkers.co.za/uie) |\n| **Totaal** | | **R24.99** |\n\n"
                        + "| Winkel | Tak | Webwerf |\n|---|---|---|\n| Checkers | Andringa Street | [checkers.co.za](https://www.checkers.co.za/) |\n\n"
                        + "| Wat | Hoekom |\n|---|---|\n| Alle pryse | R0 gevind |\n";

        Assert.That(Prices.Priced(answer), Is.EqualTo(new PriceCount(1, 2, 0, 0)));
    }

    [Test]
    public void ATallyAlone_RatesNothing() =>
        Assert.That(Prices.Priced("...\nGeprys: 7 van 9 bestanddele"), Is.Null);

    [Test]
    public void NoList_IsNoRating() =>
        Assert.That(Prices.Priced("Net 'n resep, geen lys."), Is.Null);

    [Test]
    public void APriceThatSaysPerKg_IsNoPacksPrice()
    {
        // The cook-en row as the run wrote it (review of PR #58).
        string answer = HeaderEn + $"| Chicken thighs (Farmer's Choice, 8 piece) | 700 g | 1 kg | [R99,99/kg]({Thighs}) | - | - | R99,99 Checkers |\n";

        Assert.That(Prices.Priced(answer), Is.EqualTo(new PriceCount(0, 1, 0, 1)));
    }

    [Test]
    public void OnePriceForAProductSoldByWeight_IsItsPricePerKg()
    {
        // The cook-shops row: a 1–1,2 kg pack at R99,99, the price per kg (Checkers' link ends in the unit, KG).
        string answer = HeaderAf + $"| Hoender, bo-been dye en boude | 1 kg | 1–1,2 kg pak | [R99,99]({Thighs}) | - | R99,99 Checkers |\n";

        Assert.That(Prices.Priced(answer), Is.EqualTo(new PriceCount(0, 1, 0, 1)));
    }

    [Test]
    public void ARangeForAProductSoldByWeight_IsItsPacksPrice()
    {
        string answer = HeaderAf + $"| Hoender | 1 kg | 1–1,2 kg pak | [R99,99–R119,99]({Thighs}) | - | R119,99 Checkers |\n"
                        + $"| Hoender | 1 kg | 1–1,2 kg pak | [R99,99 – 119,99]({Thighs}) | - | R119,99 Checkers |\n";

        Assert.That(Prices.Priced(answer), Is.EqualTo(new PriceCount(2, 2, 0, 0)));
    }

    [Test]
    public void OnePriceForAProductSoldEach_IsItsPacksPrice()
    {
        string answer = HeaderAf + "| Uie | 2 | 1 kg sak | [R34,99](https://www.checkers.co.za/product/onions-1kg-10135699EA) | - | R34,99 Checkers |\n";

        Assert.That(Prices.Priced(answer), Is.EqualTo(new PriceCount(1, 1, 0, 0)));
    }

    [Test]
    public void UnconfirmedMarks_AreCounted() =>
        Assert.That(Prices.UnconfirmedMarks("Prys onbekend; die ander is geskat uit die soekindeks."), Is.EqualTo(3));
}
