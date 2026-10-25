using LouisAgent.Evals.Puls.Shared.Scoring;

namespace LouisAgent.Evals.Puls.Tests.Shared.Scoring;

// F18 step 5.7: with no shop's prices looked up, the user's shop is named under Where to buy with a place (`Pick n Pay Malmesbury`, `PnP in Paarl Mall`), in
// either language. Written first: `Shops.NamesBranch` did not exist.
public class ShopsBranchTests
{
    private const string Heading = "### Where to buy\n\n";

    [TestCase("Pick n Pay Malmesbury", "pnp")]
    [TestCase("PnP in Paarl Mall", "pnp")]
    [TestCase("Pick n Pay se tak in Malmesbury, Voortrekker Street", "pnp")]
    [TestCase("Woolworths Paarl Mall", "woolworths")]
    [TestCase("Checkers te Stellenbosch", "checkers")]
    public void AShopNamedWithAPlace_IsABranch(string line, string shop) =>
        Assert.That(Shops.NamesBranch(Heading + line, shop), Is.True);

    [Test]
    public void ATableRowWhoseBranchCellHasThePlace_IsABranch() =>
        Assert.That(Shops.NamesBranch(Heading + "| Shop | Branch | Street or mall |\n|---|---|---|\n| Pick n Pay | Pick n Pay Malmesbury | Voortrekker Street |\n", "pnp"), Is.True);

    [Test]
    public void TheAfrikaansHeading_Counts() =>
        Assert.That(Shops.NamesBranch("### Waar om te koop\n\nPnP in Paarl Mall\n", "pnp"), Is.True);

    [TestCase("Any supermarket near you.")]
    [TestCase("Enige supermark naby jou.")]
    [TestCase("Pick n Pay")]
    [TestCase("Nearest Pick n Pay branch")]
    [TestCase("| Pick n Pay | nearest Pick n Pay | | [pnp.co.za](https://www.pnp.co.za) |")]
    public void AShopWithNoPlace_IsNoBranch(string line) =>
        Assert.That(Shops.NamesBranch(Heading + line, "pnp"), Is.False);

    [Test]
    public void AnotherShopsBranch_IsNoBranchOfThisShop() =>
        Assert.That(Shops.NamesBranch(Heading + "Woolworths Paarl Mall", "pnp"), Is.False);

    [Test]
    public void ABranchOutsideTheWhereToBuySection_DoesNotCount() =>
        Assert.That(Shops.NamesBranch("### Recipe\n\nBuy at Pick n Pay Malmesbury.\n\n" + Heading + "Any supermarket near you.", "pnp"), Is.False);

    [Test]
    public void NoWhereToBuySection_IsNoBranch() =>
        Assert.That(Shops.NamesBranch("Pick n Pay Malmesbury", "pnp"), Is.False);
}
