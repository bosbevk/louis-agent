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
        Assert.That(
            Shops.NamesBranch(
                Heading +
                "| Shop | Branch | Street or mall |\n|---|---|---|\n| Pick n Pay | Pick n Pay Malmesbury | Voortrekker Street |\n",
                "pnp"), Is.True);

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
        Assert.That(
            Shops.NamesBranch("### Recipe\n\nBuy at Pick n Pay Malmesbury.\n\n" + Heading + "Any supermarket near you.",
                "pnp"), Is.False);

    [Test]
    public void NoWhereToBuySection_IsNoBranch() =>
        Assert.That(Shops.NamesBranch("Pick n Pay Malmesbury", "pnp"), Is.False);

    // FX24 follow-up (review of 0ea184ef1953): the grader reads Puls's reply, which rewrites the cook's report in its own voice, and a probe found four ways a person reads
    // "the branch is named" that it rejected: a bold line for the heading, an emoji or a number before the words, and an em dash between the shop and its place.
    private const string Table = "| Shop | Branch | Street or mall |\n|---|---|---|\n";

    [TestCase("**Where to buy**\n\nPick n Pay Menlyn")]
    [TestCase("**Waar om te koop**\n\nPnP in Menlyn")]
    [TestCase("### 🛒 Where to buy\n\nPick n Pay Menlyn")]
    [TestCase("## 4. Where to buy\n\nPick n Pay Menlyn")]
    [TestCase("### Where to buy\n\nPick n Pay — Brooklyn Mall")]
    [TestCase("### Waar om te koop\n\nPick n Pay – Brooklyn Mall")]
    public void AHeadingInAnyOfTheShapesAPersonReads_AndAnEmDash_StillNameTheBranch(string answer) =>
        Assert.That(Shops.NamesBranch(answer, "pnp"), Is.True);

    [TestCase("| PnP | Pick n Pay Menlyn | Menlyn Park |")]
    [TestCase("| Pick n Pay | | Menlyn Park |")]
    [TestCase("| Pick n Pay | Pick n Pay Menlyn | |")]
    public void ATableRowOfTheShop_WhoseBranchOrStreetCellHoldsAPlace_NamesTheBranch(string row) =>
        Assert.That(Shops.NamesBranch(Heading + Table + row + "\n", "pnp"), Is.True);

    [Test]
    public void TheTableIsReadByItsColumns_InAfrikaansToo() =>
        Assert.That(
            Shops.NamesBranch(
                "### Waar om te koop\n\n| Winkel | Tak | Straat |\n|---|---|---|\n| Pick n Pay | Pick n Pay Menlyn | Atterburystraat |\n",
                "pnp"), Is.True);

    [TestCase("| Pick n Pay | nearest Pick n Pay | |", BranchFinding.OnlyNearest)]
    [TestCase("| Pick n Pay | naaste Pick n Pay | |", BranchFinding.OnlyNearest)]
    [TestCase("| Woolworths | Woolworths Menlyn | Menlyn Park |", BranchFinding.NoBranchOfShop)]
    [TestCase("| Pick n Pay | | |", BranchFinding.NoBranchOfShop)]
    public void ATableRowThatNamesNoPlace_OrAnotherShop_IsNoBranch(string row, BranchFinding expected) =>
        Assert.That(BranchReading.Of(Heading + Table + row + "\n", "pnp"), Is.EqualTo(expected));

    [Test]
    public void TheTableDecides_WhenThereIsOne_SoAnotherShopsBranchInTheTextBesideItDoesNotCount() =>
        Assert.That(
            BranchReading.Of(Heading + "Woolworths Paarl Mall\n\n" + Table + "| Pick n Pay | nearest Pick n Pay | |\n",
                "pnp"), Is.EqualTo(BranchFinding.OnlyNearest));

    [TestCase("### Recipe\n\nPick n Pay Menlyn", BranchFinding.NoSection)]
    [TestCase("### Where to buy\n\nAny supermarket near you.", BranchFinding.NoBranchOfShop)]
    [TestCase("### Where to buy\n\nPick n Pay", BranchFinding.NoBranchOfShop)]
    [TestCase("### Where to buy\n\nNearest Pick n Pay branch", BranchFinding.OnlyNearest)]
    [TestCase("### Where to buy\n\nPick n Pay Menlyn", BranchFinding.Named)]
    public void TheFinding_SaysWhichCaseItIs(string answer, BranchFinding expected) =>
        Assert.That(BranchReading.Of(answer, "pnp"), Is.EqualTo(expected));

    [Test]
    public void ABoldLineEndsTheSection_AsAHeadingDoes() =>
        Assert.That(BranchReading.Of("**Where to buy**\n\nAny supermarket.\n\n**Unsure**\n\nPick n Pay Menlyn", "pnp"),
            Is.EqualTo(BranchFinding.NoBranchOfShop));
}
