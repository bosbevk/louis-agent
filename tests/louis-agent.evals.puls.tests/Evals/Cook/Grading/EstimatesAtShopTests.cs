using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Trials;
using LouisAgent.SessionStream;
using LouisAgent.Evals.Puls.Evals.Cook.Grading;
using static LouisAgent.Evals.Puls.Tests.PulsTrials;

namespace LouisAgent.Evals.Puls.Tests.Evals.Cook.Grading;

// F18 step 5.7 (Louis, 2026-10-25: "keep cook but just rework it to not lookup prices but just focus on local recipes and where to shop"): a scenario that names
// a shop passes when the answer reads no shop page and calls no shop tool, links no price, says the prices are estimates, and names the shop's branch under
// Where to buy. `any` and `no-branch` keep their rules (PricedAtShopTests, PricedAtNoBranchTests). Written first: the grader still wanted the shop's page read.
public class EstimatesAtShopTests
{
    private const string Pnp = """{"town":"Malmesbury","shop":"pnp"}""";
    private const string Woolworths = """{"town":"Stellenbosch","shop":"woolworths"}""";
    private const string Checkers = """{"town":"Malmesbury","shop":"checkers"}""";

    private const string Estimates = "| Ingredient | For the recipe | To buy | Estimated price |\n|---|---|---|---|\n| Uie | 2 | onions 1 kg | ± R25 |\n";
    private const string EstimatesLine = "*Prices are estimates, not checked at a shop.*\n";
    private const string PnpBranch = "\n### Where to buy\n\nPick n Pay Malmesbury, Voortrekker Street\n";
    private const string WoolworthsBranch = "\n### Waar om te koop\n\nWoolworths Eikestad Mall, Stellenbosch\n";
    private const string Priced = "| Ingredient | For the recipe | To buy | PnP |\n|---|---|---|---|\n| Uie | 2 | Uie 1 kg | [R47,00](https://www.pnp.co.za/p/1) |\n";

    private static readonly SessionEvent ReadsPnp = SpecialistCall("ReadRenderedPage", """{"url":"https://www.pnp.co.za/search/uie","maxChars":3000}""");
    private static readonly SessionEvent FetchesPnp = SpecialistCall("FetchUrl", """{"url":"https://www.pnp.co.za/search/uie"}""");
    private static readonly SessionEvent ReadsRecipe = SpecialistCall("FetchUrl", """{"url":"https://marindakook.co.za/bobotie/"}""");

    private static Task<Grade> Grade(string expected, params SessionEvent[] events) =>
        new EstimatesAtShop().GradeAsync(Trial(CookMode, "cook-x", expected, null, Turn(1, events)), CancellationToken.None).AsTask();

    [Test]
    public void ItIsARequiredFixedRule_ThatReadsTheShopAndTheTownKeys()
    {
        var grader = new EstimatesAtShop();

        Assert.That((grader.Name, grader.Version, grader.Kind, grader.Required), Is.EqualTo(("estimates-at-shop", 2, GraderKind.FixedRule, true)));
        Assert.That(grader.ExpectedKeys, Is.EquivalentTo(new[] { "shop", "town" }));
    }

    [Test]
    public async Task PnP_Passes_WhenNoShopPageWasReadNoPriceIsLinkedTheEstimatesAreSaidAndTheBranchIsNamed() =>
        Assert.That((await Grade(Pnp, ReadsRecipe, Text(Estimates + EstimatesLine + PnpBranch))).Outcome, Is.EqualTo(GradeOutcome.Pass));

    [Test]
    public async Task Woolworths_Passes_WithTheAfrikaansLinesAndHeading() =>
        Assert.That((await Grade(Woolworths, Text(Estimates + "*Pryse is skattings, nie by 'n winkel nagegaan nie.*\n" + WoolworthsBranch))).Outcome,
            Is.EqualTo(GradeOutcome.Pass));

    [Test]
    public async Task Checkers_Passes_TheSameWay_BecauseNoShopIsPricedNow() =>
        Assert.That((await Grade(Checkers, Text(Estimates + EstimatesLine + "\n### Where to buy\n\nCheckers Malmesbury\n"))).Outcome, Is.EqualTo(GradeOutcome.Pass));

    [Test]
    public async Task ItFails_WhenAShopPageWasReadWithReadRenderedPage()
    {
        var grade = await Grade(Pnp, ReadsPnp, Text(Estimates + EstimatesLine + PnpBranch));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("page"));
    }

    [Test]
    public async Task ItFails_WhenAShopPageWasFetchedWithFetchUrl() =>
        Assert.That((await Grade(Pnp, FetchesPnp, Text(Estimates + EstimatesLine + PnpBranch))).Outcome, Is.EqualTo(GradeOutcome.Fail));

    [Test]
    public async Task ItFails_WhenAShopToolWasCalled() =>
        Assert.That((await Grade(Pnp, SpecialistCall("FindProducts", """{"shop":"checkers","area":"Malmesbury","query":"uie"}"""),
            Text(Estimates + EstimatesLine + PnpBranch))).Outcome, Is.EqualTo(GradeOutcome.Fail));

    [Test]
    public async Task ItFails_WhenAPriceLinksToAShop()
    {
        var grade = await Grade(Pnp, Text(Priced + EstimatesLine + PnpBranch));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("price"));
    }

    [Test]
    public async Task ItFails_WhenTheEstimatesLineIsMissing()
    {
        var grade = await Grade(Pnp, Text(Estimates + PnpBranch));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("estimates"));
    }

    [Test]
    public async Task ItFails_WhenNoBranchIsNamed()
    {
        var grade = await Grade(Pnp, Text(Estimates + EstimatesLine + "\n### Where to buy\n\nAny supermarket near you.\n"));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("branch"));
    }

    [Test]
    public async Task ItIsUnknown_ForAnInfrastructureFailure()
    {
        var failed = Trial(CookMode, "cook-x", Pnp, null, Turn(1, [Text(Estimates + EstimatesLine + PnpBranch)], TrialFailure.Timeout));

        Assert.That((await new EstimatesAtShop().GradeAsync(failed, CancellationToken.None)).Outcome, Is.EqualTo(GradeOutcome.Unknown));
    }

    // Version 2 (review of 0ea184ef1953): a fail says which case it is, so it can be told apart without the answer, and the branch is read the way a person reads it.
    [Test]
    public async Task ItFails_SayingThereIsNoWhereToBuySection_WhenThereIsNone()
    {
        var grade = await Grade(Pnp, ReadsRecipe, Text(Estimates + EstimatesLine + "\n### Recipe\n\nPick n Pay Malmesbury\n"));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("no Where to buy section"));
    }

    [Test]
    public async Task ItFails_SayingNoBranchOfTheShopWithAPlace_WhenTheSectionNamesNone()
    {
        var grade = await Grade(Pnp, ReadsRecipe, Text(Estimates + EstimatesLine + "\n### Where to buy\n\nAny supermarket near you.\n"));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("no pnp branch with a place"));
    }

    [Test]
    public async Task ItFails_SayingOnlyANearestBranch_WhenThatIsAllItGives()
    {
        var grade = await Grade(Pnp, ReadsRecipe, Text(Estimates + EstimatesLine
            + "\n### Where to buy\n\n| Shop | Branch | Street or mall |\n|---|---|---|\n| Pick n Pay | nearest Pick n Pay | |\n"));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("only a nearest pnp branch"));
    }

    [Test]
    public async Task ItPasses_WhenTheTableOfAPlainBoldHeadingNamesTheBranch() =>
        Assert.That((await Grade(Pnp, ReadsRecipe, Text(Estimates + EstimatesLine
            + "\n**Where to buy**\n\n| Shop | Branch | Street or mall |\n|---|---|---|\n| PnP | Pick n Pay Menlyn | Menlyn Park |\n"))).Outcome, Is.EqualTo(GradeOutcome.Pass));
}
