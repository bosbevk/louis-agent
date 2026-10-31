using LouisAgent.Packs.Shops.Checks.Reading;

namespace LouisAgent.Packs.Shops.Tests.Checks.Reading;

/// <summary>
/// With no shop's prices looked up, the user's shop is named under Where to buy with a place (<c>Pick n Pay
/// Malmesbury</c>, <c>PnP in Paarl Mall</c>), in either language.
/// </summary>
public class BranchReadingTests
{
    /// <summary>The start of the Where to buy section.</summary>
    private const string Heading = "### Where to buy\n\n";

    /// <summary>A shop named with a place, in any of the ways a person writes it, is a branch.</summary>
    [TestCase("Pick n Pay Malmesbury", "pnp")]
    [TestCase("PnP in Paarl Mall", "pnp")]
    [TestCase("Pick n Pay se tak in Malmesbury, Voortrekker Street", "pnp")]
    [TestCase("Woolworths Paarl Mall", "woolworths")]
    [TestCase("Checkers te Stellenbosch", "checkers")]
    public void AShopNamedWithAPlace_IsABranch(string line, string shop) =>
        Assert.That(ShopLines.NamesBranch(Heading + line, shop), Is.True);

    /// <summary>A table row whose branch cell holds the place is a branch.</summary>
    [Test]
    public void ATableRowWhoseBranchCellHasThePlace_IsABranch() =>
        Assert.That(
            ShopLines.NamesBranch(
                Heading +
                "| Shop | Branch | Street or mall |\n|---|---|---|\n| Pick n Pay | Pick n Pay Malmesbury | Voortrekker Street |\n",
                "pnp"), Is.True);

    /// <summary>The section's Afrikaans heading counts as well.</summary>
    [Test]
    public void TheAfrikaansHeading_Counts() =>
        Assert.That(ShopLines.NamesBranch("### Waar om te koop\n\nPnP in Paarl Mall\n", "pnp"), Is.True);

    /// <summary>A shop with no place, or only "the nearest one", is no branch.</summary>
    [TestCase("Any supermarket near you.")]
    [TestCase("Enige supermark naby jou.")]
    [TestCase("Pick n Pay")]
    [TestCase("Nearest Pick n Pay branch")]
    [TestCase("| Pick n Pay | nearest Pick n Pay | | [pnp.co.za](https://www.pnp.co.za) |")]
    public void AShopWithNoPlace_IsNoBranch(string line) =>
        Assert.That(ShopLines.NamesBranch(Heading + line, "pnp"), Is.False);

    /// <summary>Another shop's branch is no branch of the user's shop.</summary>
    [Test]
    public void AnotherShopsBranch_IsNoBranchOfThisShop() =>
        Assert.That(ShopLines.NamesBranch(Heading + "Woolworths Paarl Mall", "pnp"), Is.False);

    /// <summary>A branch named outside the Where to buy section doesn't count.</summary>
    [Test]
    public void ABranchOutsideTheWhereToBuySection_DoesNotCount() =>
        Assert.That(
            ShopLines.NamesBranch(
                "### Recipe\n\nBuy at Pick n Pay Malmesbury.\n\n" + Heading + "Any supermarket near you.",
                "pnp"), Is.False);

    /// <summary>An answer with no Where to buy section names no branch.</summary>
    [Test]
    public void NoWhereToBuySection_IsNoBranch() =>
        Assert.That(ShopLines.NamesBranch("Pick n Pay Malmesbury", "pnp"), Is.False);

    /// <summary>The top of a Where to buy table, with its branch and street columns.</summary>
    private const string Table = "| Shop | Branch | Street or mall |\n|---|---|---|\n";

    /// <summary>
    /// The check reads Puls's reply, which rewrites the cook's report in its own voice: a bold line for the heading, an
    /// emoji or a number before its words, or a dash between the shop and its place still names the branch.
    /// </summary>
    [TestCase("**Where to buy**\n\nPick n Pay Menlyn")]
    [TestCase("**Waar om te koop**\n\nPnP in Menlyn")]
    [TestCase("### 🛒 Where to buy\n\nPick n Pay Menlyn")]
    [TestCase("## 4. Where to buy\n\nPick n Pay Menlyn")]
    [TestCase("### Where to buy\n\nPick n Pay — Brooklyn Mall")]
    [TestCase("### Waar om te koop\n\nPick n Pay – Brooklyn Mall")]
    public void AHeadingInAnyOfTheShapesAPersonReads_AndAnEmDash_StillNameTheBranch(string answer) =>
        Assert.That(ShopLines.NamesBranch(answer, "pnp"), Is.True);

    /// <summary>A table row of the user's shop whose branch or street cell holds a place names the branch.</summary>
    [TestCase("| PnP | Pick n Pay Menlyn | Menlyn Park |")]
    [TestCase("| Pick n Pay | | Menlyn Park |")]
    [TestCase("| Pick n Pay | Pick n Pay Menlyn | |")]
    public void ATableRowOfTheShop_WhoseBranchOrStreetCellHoldsAPlace_NamesTheBranch(string row) =>
        Assert.That(ShopLines.NamesBranch(Heading + Table + row + "\n", "pnp"), Is.True);

    /// <summary>The table is read by its columns, with Afrikaans headings too.</summary>
    [Test]
    public void TheTableIsReadByItsColumns_InAfrikaansToo() =>
        Assert.That(
            ShopLines.NamesBranch(
                "### Waar om te koop\n\n| Winkel | Tak | Straat |\n|---|---|---|\n| Pick n Pay | Pick n Pay Menlyn | Atterburystraat |\n",
                "pnp"), Is.True);

    /// <summary>A row with no place, or another shop's branch, is no branch, and the finding says which.</summary>
    [TestCase("| Pick n Pay | nearest Pick n Pay | |", BranchFinding.OnlyNearest)]
    [TestCase("| Pick n Pay | naaste Pick n Pay | |", BranchFinding.OnlyNearest)]
    [TestCase("| Woolworths | Woolworths Menlyn | Menlyn Park |", BranchFinding.NoBranchOfShop)]
    [TestCase("| Pick n Pay | | |", BranchFinding.NoBranchOfShop)]
    public void ATableRowThatNamesNoPlace_OrAnotherShop_IsNoBranch(string row, BranchFinding expected) =>
        Assert.That(BranchReading.Of(Heading + Table + row + "\n", "pnp"), Is.EqualTo(expected));

    /// <summary>A table decides, so another shop's branch in the text beside it doesn't count.</summary>
    [Test]
    public void TheTableDecides_WhenThereIsOne_SoAnotherShopsBranchInTheTextBesideItDoesNotCount() =>
        Assert.That(
            BranchReading.Of(Heading + "Woolworths Paarl Mall\n\n" + Table + "| Pick n Pay | nearest Pick n Pay | |\n",
                "pnp"), Is.EqualTo(BranchFinding.OnlyNearest));

    /// <summary>The finding says which case it is: no section, no branch, only the nearest, or named.</summary>
    [TestCase("### Recipe\n\nPick n Pay Menlyn", BranchFinding.NoSection)]
    [TestCase("### Where to buy\n\nAny supermarket near you.", BranchFinding.NoBranchOfShop)]
    [TestCase("### Where to buy\n\nPick n Pay", BranchFinding.NoBranchOfShop)]
    [TestCase("### Where to buy\n\nNearest Pick n Pay branch", BranchFinding.OnlyNearest)]
    [TestCase("### Where to buy\n\nPick n Pay Menlyn", BranchFinding.Named)]
    public void TheFinding_SaysWhichCaseItIs(string answer, BranchFinding expected) =>
        Assert.That(BranchReading.Of(answer, "pnp"), Is.EqualTo(expected));

    /// <summary>A bold line ends the section, as a heading does.</summary>
    [Test]
    public void ABoldLineEndsTheSection_AsAHeadingDoes() =>
        Assert.That(BranchReading.Of("**Where to buy**\n\nAny supermarket.\n\n**Unsure**\n\nPick n Pay Menlyn", "pnp"),
            Is.EqualTo(BranchFinding.NoBranchOfShop));
}
