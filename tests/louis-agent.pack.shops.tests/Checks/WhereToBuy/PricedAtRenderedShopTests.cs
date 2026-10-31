using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Packs.Shops.Checks;
using LouisAgent.SessionStream;
using static LouisAgent.Packs.Shops.Tests.Checks.ShopTrials;

namespace LouisAgent.Packs.Shops.Tests.Checks.WhereToBuy;

/// <summary>
/// A shop with no catalogue tool (PnP, Woolworths) is priced by reading its search page in the browser, as the cook's
/// prompt says: the page read is on the shop's site and the prices link to it. There's no <c>area</c> to name, so the
/// town isn't asked for. Checkers' own rules are in <c>PricedAtShopTests</c>. Which sites the prices link to is the
/// reader's to say: each case's fake reads what Puls's <c>rand-prices</c> reads in its list.
/// </summary>
public class PricedAtRenderedShopTests
{
    /// <summary>A scenario at Malmesbury's PnP.</summary>
    private const string Pnp = """{"town":"Malmesbury","shop":"pnp"}""";

    /// <summary>A scenario at Stellenbosch's Woolworths.</summary>
    private const string Woolworths = """{"town":"Stellenbosch","shop":"woolworths"}""";

    /// <summary>The cook reading a PnP search page in the browser.</summary>
    private static readonly SessionEvent ReadsPnp = SpecialistCall("ReadRenderedPage",
        """{"url":"https://www.pnp.co.za/search/chicken","maxChars":3000}""");

    /// <summary>The cook reading a Woolworths search page in the browser.</summary>
    private static readonly SessionEvent ReadsWoolworths =
        SpecialistCall("ReadRenderedPage",
            """{"url":"https://www.woolworths.co.za/browse?searchterm=chicken&fr=1","maxChars":3000}""");

    /// <summary>A list priced at PnP, its price linked to PnP's site.</summary>
    private const string PnpList =
        "| Ingredient | For the recipe | To buy | PnP |\n|---|---|---|---|\n| Uie | 2 | Uie 1 kg | [R47,00](https://www.pnp.co.za/p/1) |\n";

    /// <summary>A list priced at Woolworths, its price linked to Woolworths' site.</summary>
    private const string WoolworthsList =
        "| Ingredient | For the recipe | To buy | Woolworths |\n|---|---|---|---|\n| Uie | 2 | Uie 1 kg | [R52,00](https://www.woolworths.co.za/p/1) |\n";

    /// <summary>What Puls's reader reads in the PnP list: one price as fact, linked to PnP.</summary>
    private static readonly FakePrices AtPnp = new(1, "pnp.co.za");

    /// <summary>What Puls's reader reads in the Woolworths list: one price as fact, linked to Woolworths.</summary>
    private static readonly FakePrices AtWoolworths = new(1, "woolworths.co.za");

    /// <summary>The priced-at-shop check's grade of a one-turn trial made of these events, over this reader.</summary>
    private static Task<Grade> Grade(FakePrices prices, string expected, params SessionEvent[] events) =>
        new PricedAtRetailer(prices).GradeAsync(Trial(expected, Turn(1, events)), CancellationToken.None).AsTask();

    /// <summary>PnP's page read and the prices linked to its site: a pass.</summary>
    [Test]
    public async Task PnP_Passes_WhenItsPageWasReadAndThePricesLinkToItsSite() =>
        Assert.That((await Grade(AtPnp, Pnp, ReadsPnp, Text(PnpList))).Outcome, Is.EqualTo(GradeOutcome.Pass));

    /// <summary>The same holds for Woolworths.</summary>
    [Test]
    public async Task Woolworths_Passes_WhenItsPageWasReadAndThePricesLinkToItsSite() =>
        Assert.That((await Grade(AtWoolworths, Woolworths, ReadsWoolworths, Text(WoolworthsList))).Outcome,
            Is.EqualTo(GradeOutcome.Pass));

    /// <summary>Prices with no page of the shop read fail, naming the shop.</summary>
    [Test]
    public async Task ItFails_WhenNoPageOfTheShopWasRead()
    {
        var grade = await Grade(AtPnp, Pnp, Text(PnpList));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("pnp"));
    }

    /// <summary>Reading only another shop's page fails.</summary>
    [Test]
    public async Task ItFails_WhenOnlyAnotherShopsPageWasRead() =>
        Assert.That((await Grade(AtPnp, Pnp, ReadsWoolworths, Text(PnpList))).Outcome, Is.EqualTo(GradeOutcome.Fail));

    /// <summary>Prices that link to another shop fail, naming the shop asked for.</summary>
    [Test]
    public async Task ItFails_WhenThePricesDoNotLinkToTheShop()
    {
        var grade = await Grade(AtWoolworths, Pnp, ReadsPnp, Text(WoolworthsList));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("pnp"));
    }

    /// <summary>With no shop chosen, reading a shop's page fails: the cook's prompt says to read none.</summary>
    [Test]
    public async Task WithNoShopChosen_APageOfAShopRead_Fails_BecauseTheCooksPromptSaysNoShopPage()
    {
        var grade = await new EstimatesAtShop(FakePrices.None).GradeAsync(
            Trial("""{"town":"Paarl","shop":"any"}""",
                Turn(1, [ReadsPnp, Text("Resep.\n*Pryse is skattings, nie by 'n winkel nagegaan nie.*")])),
            CancellationToken.None);

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("page"));
    }
}
