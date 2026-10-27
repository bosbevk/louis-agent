using LouisAgent.Evals.Puls.Shared.Scoring;

namespace LouisAgent.Evals.Puls.Tests.Shared.Scoring;

// The Python's test_scoring.py, the shop rules (F34): the deals a row names, the shops with no tool, the catalogue line, and
// which store answered a search.
public class ShopsTests
{
    private const string OneShop = "| Ingredient | For the recipe | To buy | Checkers |\n|---|---|---|---|\n";

    [Test]
    public void ADealNamedBesideAPrice_IsCounted()
    {
        string answer =
            OneShop + "| Rice | 300 g | Spekko Rice 1 kg | [R24,99](https://www.checkers.co.za/p/1) · BUY 2 FOR R45 |\n"
                    + "| Chicken | 1 kg | Farmer's Choice 16-piece | [R135,98–R151,98](https://www.checkers.co.za/p/2KG) · SAVE |\n"
                    + "| Onions | 2 | Onions 1 kg | [R34,99](https://www.checkers.co.za/p/3) |\n";

        Assert.That(Shops.DealsNamed(answer), Is.EqualTo(2));
    }

    [Test]
    public void NoDeal_IsNoneNamed() =>
        Assert.That(
            Shops.DealsNamed(OneShop + "| Onions | 2 | Onions 1 kg | [R34,99](https://www.checkers.co.za/p/3) |\n"),
            Is.EqualTo(0));

    [Test]
    public void AShopWithoutATool_IsCountedInTheList()
    {
        // Checkers has a tool (FindProducts) and PnP and Woolworths a rendered page (ReadRenderedPage); Shoprite has neither: its column breaks the rule.
        string answer =
            "| Ingredient | For the recipe | To buy | Checkers | Pick n Pay | Shoprite | Best price |\n|---|---|---|---|---|---|---|\n"
            + "| Onions | 2 | Onions 1 kg | [R34,99](https://www.checkers.co.za/p/3) | - | - | R34,99 Checkers |\n";

        Assert.That(Shops.UntooledShops(answer), Is.EqualTo(1));
        Assert.That(
            Shops.UntooledShops(OneShop.Replace("Checkers", "Woolworths") + "| Onions | 2 | Onions 1 kg | - |\n"),
            Is.EqualTo(0));
        Assert.That(Shops.UntooledShops(OneShop + "| Onions | 2 | Onions 1 kg | - |\n"), Is.EqualTo(0));
    }

    [Test]
    public void TheNoCatalogueLine_IsFoundInEitherLanguage()
    {
        Assert.That(Shops.NoCatalogueLine("...\n_No shop near you has a catalogue here yet._"), Is.True);
        Assert.That(Shops.NoCatalogueLine("...\n*Geen winkel naby jou het nog 'n katalogus hier nie.*"), Is.True);
        Assert.That(Shops.NoCatalogueLine("Geprys: 3 van 3 bestanddele"), Is.False);
    }

    [Test]
    public void WhichStoreAnswered_IsCountedFromTheToolsHeaders()
    {
        // The tool names the store that answered, checked against Checkers' own store id; a search the town's store didn't answer says so.
        var results = new[]
        {
            "Products at Checkers (the store serving Malmesbury, store 655b0c8f) for 'rice' (1), from the shop's own catalogue",
            "Specials (multi-buys) at Checkers (the store serving Malmesbury, store 655b0c8f) for 'coffee' (2)",
            "Products at Checkers (its default store, store 5ece6935: Malmesbury's wasn't used) for 'onions' (1)",
            "Products at Checkers (its default store: no area shared) for 'eggs' (3)",
            "Saturday 17 October 2026",
        };

        Assert.That((Shops.StoreForArea(results), Shops.StoreDefault(results)), Is.EqualTo((2, 2)));
    }
}
