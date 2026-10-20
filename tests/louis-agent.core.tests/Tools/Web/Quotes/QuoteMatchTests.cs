using LouisAgent.Core.Tools.Web.Quotes;

namespace LouisAgent.Core.Tests.Tools.Web.Quotes;

// S (CL15): the rules VerifyQuote matches a quote by, each FX16 lesson on its own: how text is normalised, what counts as
// only a price, when a price stands for a product, and what a JavaScript shell looks like.
public class QuoteMatchTests
{
    [Test]
    public void Normalised_IsDecoded_OneSpaced_LowerCase_WithTheRandSignJoined() =>
        Assert.That(QuoteMatch.Normalise("Beef&nbsp;Mince  \n R 35.99 &amp; more"), Is.EqualTo("beef mince r35.99 & more"));

    [TestCase("r22.99", true)]
    [TestCase("r22.99 each", true)]
    [TestCase("now r 1 299,00 per kg", true)]
    [TestCase("beef mince r22.99", false)]
    [TestCase("eggs", false)]
    public void AQuoteThatIsOnlyAPrice_NamesNoProduct(string quote, bool onlyAPrice) =>
        Assert.That(QuoteMatch.IsOnlyAPrice(quote), Is.EqualTo(onlyAPrice));

    [Test]
    public void ThePrice_StandsForTheProduct_WhenItIsTheNextPriceAfterItsName()
    {
        const string page = "specials beef mince 500g r59.99 chicken fillets r45.00";

        Assert.That(QuoteMatch.StandsAfter(page, "beef mince", "r59.99"), Is.True);
        Assert.That(QuoteMatch.StandsAfter(page, "chicken fillets", "r45.00"), Is.True);
        Assert.That(QuoteMatch.StandsAfter(page, "chicken fillets", "r59.99"), Is.False, "the price before the name is another product's");
        Assert.That(QuoteMatch.StandsAfter(page, "beef mince", "r45.00"), Is.False, "another price stands between");
    }

    [Test]
    public void ASpecialsOldPrice_IsSkipped_AndIsNeverThePriceItself()
    {
        const string page = "beef mince 500g was r69.99 save r10.00 now r59.99";

        Assert.That(QuoteMatch.StandsAfter(page, "beef mince", "r59.99"), Is.True);
        Assert.That(QuoteMatch.StandsAfter(page, "beef mince", "r69.99"), Is.False, "the old price");
        Assert.That(QuoteMatch.StandsAfter(page, "beef mince", "r10.00"), Is.False, "the saving");
    }

    [Test]
    public void AQuoteThatHoldsTheName_OnlyHasToBeOnThePage() =>
        Assert.That(QuoteMatch.StandsAfter("x beef mince r59.99 y", "beef mince", "beef mince r59.99"), Is.True);

    [Test]
    public void AShell_HasNoText_OrALargeBodyWithOnlyAMenu()
    {
        Assert.That(QuoteMatch.LooksLikeShell(readable: "", all: " ", bodyLength: 100), Is.True);
        Assert.That(QuoteMatch.LooksLikeShell(readable: "home specials loading…", all: "home specials loading…", bodyLength: 30_000), Is.True);
        Assert.That(QuoteMatch.LooksLikeShell(readable: new string('a', 400), all: new string('a', 400), bodyLength: 30_000), Is.False);
        Assert.That(QuoteMatch.LooksLikeShell(readable: "home", all: "home r59.99", bodyLength: 500), Is.False, "a small page is what it is");
    }
}
