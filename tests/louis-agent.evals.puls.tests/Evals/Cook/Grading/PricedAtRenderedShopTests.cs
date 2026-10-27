using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Trials;
using LouisAgent.SessionStream;
using LouisAgent.Evals.Puls.Evals.Cook.Grading;
using static LouisAgent.Evals.Puls.Tests.PulsTrials;

namespace LouisAgent.Evals.Puls.Tests.Evals.Cook.Grading;

// A shop with no catalogue tool (PnP, Woolworths) is priced by reading its search page with ReadRenderedPage, as the cook's prompt says ("a rendered
// page"): the page read is on the shop's site and the prices link to it. There is no `area` to name, so the town is not asked for. Checkers' own rules
// are PricedAtShopTests'. Written first: PnP and Woolworths were no value of the shop, and the cook that priced at them failed "no checkers tool".
public class PricedAtRenderedShopTests
{
    private const string Pnp = """{"town":"Malmesbury","shop":"pnp"}""";
    private const string Woolworths = """{"town":"Stellenbosch","shop":"woolworths"}""";

    private static readonly SessionEvent ReadsPnp = SpecialistCall("ReadRenderedPage",
        """{"url":"https://www.pnp.co.za/search/chicken","maxChars":3000}""");

    private static readonly SessionEvent ReadsWoolworths =
        SpecialistCall("ReadRenderedPage",
            """{"url":"https://www.woolworths.co.za/browse?searchterm=chicken&fr=1","maxChars":3000}""");

    private const string PnpList =
        "| Ingredient | For the recipe | To buy | PnP |\n|---|---|---|---|\n| Uie | 2 | Uie 1 kg | [R47,00](https://www.pnp.co.za/p/1) |\n";

    private const string WoolworthsList =
        "| Ingredient | For the recipe | To buy | Woolworths |\n|---|---|---|---|\n| Uie | 2 | Uie 1 kg | [R52,00](https://www.woolworths.co.za/p/1) |\n";

    private static Task<Grade> Estimate(string expected, params SessionEvent[] events) =>
        new EstimatesAtShop()
            .GradeAsync(Trial(CookEvalMode, "cook-x", expected, null, Turn(1, events)), CancellationToken.None)
            .AsTask();

    private static Task<Grade> Grade(string expected, params SessionEvent[] events) =>
        new PricedAtRetailer()
            .GradeAsync(Trial(CookEvalMode, "cook-x", expected, null, Turn(1, events)), CancellationToken.None)
            .AsTask();

    [Test]
    public async Task PnP_Passes_WhenItsPageWasReadAndThePricesLinkToItsSite() =>
        Assert.That((await Grade(Pnp, ReadsPnp, Text(PnpList))).Outcome, Is.EqualTo(GradeOutcome.Pass));

    [Test]
    public async Task Woolworths_Passes_WhenItsPageWasReadAndThePricesLinkToItsSite() =>
        Assert.That((await Grade(Woolworths, ReadsWoolworths, Text(WoolworthsList))).Outcome,
            Is.EqualTo(GradeOutcome.Pass));

    [Test]
    public async Task ItFails_WhenNoPageOfTheShopWasRead()
    {
        var grade = await Grade(Pnp, Text(PnpList));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("pnp"));
    }

    [Test]
    public async Task ItFails_WhenOnlyAnotherShopsPageWasRead() =>
        Assert.That((await Grade(Pnp, ReadsWoolworths, Text(PnpList))).Outcome, Is.EqualTo(GradeOutcome.Fail));

    [Test]
    public async Task ItFails_WhenThePricesDoNotLinkToTheShop()
    {
        var grade = await Grade(Pnp, ReadsPnp, Text(WoolworthsList));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("pnp"));
    }

    [Test]
    public async Task WithNoShopChosen_APageOfAShopRead_Fails_BecauseTheCooksPromptSaysNoShopPage()
    {
        var grade = await Estimate("""{"town":"Paarl","shop":"any"}""", ReadsPnp,
            Text("Resep.\n*Pryse is skattings, nie by 'n winkel nagegaan nie.*"));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("page"));
    }
}
