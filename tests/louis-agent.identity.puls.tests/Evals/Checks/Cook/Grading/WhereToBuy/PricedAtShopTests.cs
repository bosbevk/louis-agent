using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Identities.Puls.Evals.Checks.Cook.Grading.WhereToBuy;
using LouisAgent.SessionStream;
using static LouisAgent.Identities.Puls.Tests.Evals.Checks.PulsTrials;

namespace LouisAgent.Identities.Puls.Tests.Evals.Checks.Cook.Grading.WhereToBuy;

// F18 step 5.4: the shop the user chose, as a fixed rule over the calls and the answer. The cook's prompt: a user's shop (Checkers) is priced with its
// tool and `area` = the user's town in every call, and the prices link to its site; when the user chose none (`any`), the cook prices nothing, reads no
// shop, and writes estimates (`± R65`, no link) under a line that says so. Written first: the grader threw NotImplementedException on the red run.
public class PricedAtShopTests
{
    private const string Checkers = """{"town":"Malmesbury","shop":"checkers"}""";
    private const string Any = """{"town":"Stellenbosch","shop":"any"}""";

    private static readonly SessionEvent FindsInMalmesbury =
        SpecialistCall("FindProducts", """{"shop":"checkers","area":"Malmesbury","query":"uie"}""");

    private const string CheckersList =
        "| Ingredient | For the recipe | To buy | Checkers |\n|---|---|---|---|\n| Uie | 2 | Uie 1 kg | [R45,99](https://www.checkers.co.za/p/1) |\n";

    private const string PnpList =
        "| Ingredient | For the recipe | To buy | Pick n Pay |\n|---|---|---|---|\n| Uie | 2 | Uie 1 kg | [R47,00](https://www.pnp.co.za/p/1) |\n";

    private const string EstimateList =
        "| Ingredient | For the recipe | To buy | Estimated price |\n|---|---|---|---|\n| Uie | 2 | onions 1 kg | ± R25 |\n";

    private const string EstimatesLine = "*Pryse is skattings, nie by 'n winkel nagegaan nie.*";

    private static Task<Grade> Estimate(string expected, params SessionEvent[] events) =>
        new EstimatesAtShop()
            .GradeAsync(Trial(CookEvalMode, "cook-x", expected, null, Turn(1, events)), CancellationToken.None)
            .AsTask();

    private static Task<Grade> Grade(string expected, params SessionEvent[] events) =>
        new PricedAtRetailer()
            .GradeAsync(Trial(CookEvalMode, "cook-x", expected, null, Turn(1, events)), CancellationToken.None)
            .AsTask();

    [Test]
    public void ItIsARequiredFixedRule_ThatReadsTheShopAndTheTownKeys()
    {
        var grader = new PricedAtRetailer();

        Assert.That((grader.Name, grader.Kind, grader.Required),
            Is.EqualTo(("priced-at-shop", CheckKind.FixedRule, true)));
        Assert.That(grader.ExpectedKeys, Is.EquivalentTo(new[] { "shop", "town" }));
    }

    // --- the user's shop

    [Test]
    public async Task WithAShop_ItPasses_WhenTheShopToolRanForTheTownAndThePricesLinkToTheShop() =>
        Assert.That((await Grade(Checkers, FindsInMalmesbury, Text(CheckersList))).Outcome,
            Is.EqualTo(GradeOutcome.Pass));

    [Test]
    public async Task WithAShop_TheAreaMatchesTheTownWhateverTheCaseOrExtraWords()
    {
        var found = SpecialistCall("SearchSpecials", """{"shop":"Checkers","area":"malmesbury, western cape"}""");

        Assert.That((await Grade(Checkers, found, Text(CheckersList))).Outcome, Is.EqualTo(GradeOutcome.Pass));
    }

    [Test]
    public async Task WithAShop_ItFails_WhenTheToolWasNeverCalled()
    {
        var grade = await Grade(Checkers, Text(CheckersList));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("checkers"));
    }

    [Test]
    public async Task WithAShop_ItFails_WhenACallNamesAnotherTown()
    {
        var elsewhere = SpecialistCall("FindProducts", """{"shop":"checkers","area":"Stellenbosch","query":"uie"}""");

        var grade = await Grade(Checkers, elsewhere, Text(CheckersList));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("Malmesbury"));
    }

    [Test]
    public async Task WithAShop_ItFails_WhenACallLeavesTheAreaOut_BecauseTheCookPromptSaysAreaInEveryCall()
    {
        var noArea = SpecialistCall("FindProducts", """{"shop":"checkers","query":"uie"}""");

        Assert.That((await Grade(Checkers, FindsInMalmesbury, noArea, Text(CheckersList))).Outcome,
            Is.EqualTo(GradeOutcome.Fail));
    }

    [Test]
    public async Task WithAShop_ItFails_WhenThePricesLinkToAnotherShop() =>
        Assert.That((await Grade(Checkers, FindsInMalmesbury, Text(PnpList))).Outcome, Is.EqualTo(GradeOutcome.Fail));

    [Test]
    public async Task WithAShop_ItFails_WhenThereIsNoPricedList() =>
        Assert.That((await Grade(Checkers, FindsInMalmesbury, Text("Ek weet nie wat dit kos nie."))).Outcome,
            Is.EqualTo(GradeOutcome.Fail));

    // --- no shop chosen

    [Test]
    public async Task WithAnyShop_ItPasses_WhenNothingWasSearchedNoPriceIsLinkedAndTheAnswerSaysTheyAreEstimates() =>
        Assert.That((await Estimate(Any, Text(EstimateList + EstimatesLine))).Outcome, Is.EqualTo(GradeOutcome.Pass));

    [Test]
    public async Task WithAnyShop_TheEnglishLineCountsToo() =>
        Assert.That((await Estimate(Any, Text(EstimateList + "Prices are estimates, not checked at a shop."))).Outcome,
            Is.EqualTo(GradeOutcome.Pass));

    [Test]
    public async Task WithAnyShop_ItFails_WhenAShopToolWasCalled()
    {
        var grade = await Estimate(Any, FindsInMalmesbury, Text(EstimateList + EstimatesLine));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("shop tool"));
    }

    [Test]
    public async Task WithAnyShop_ItFails_WhenAPriceIsLinkedToAShop()
    {
        // A price from a shop for a user who chose none: step 3's wrong_store.
        var grade = await Estimate(Any, Text(CheckersList + EstimatesLine));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("price"));
    }

    [Test]
    public async Task WithAnyShop_ItFails_WhenAPriceIsGivenAsFact_NotAsAnEstimate() =>
        Assert.That(
            (await Estimate(Any,
                Text(
                    "| Ingredient | For the recipe | To buy | Prys |\n|---|---|---|---|\n| Uie | 2 | uie | R20,00 |\n" +
                    EstimatesLine))).Outcome,
            Is.EqualTo(GradeOutcome.Fail));

    [Test]
    public async Task WithAnyShop_ItFails_WhenTheAnswerDoesNotSayTheyAreEstimates()
    {
        var grade = await Estimate(Any, Text(EstimateList));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("estimates"));
    }

    // --- every other case

    [Test]
    public async Task ItPasses_WhenTheScenarioNamesNoShop() =>
        Assert.That((await Grade("""{"routes_to":null}""", Text("Resep."))).Outcome, Is.EqualTo(GradeOutcome.Pass));

    [Test]
    public async Task ItIsUnknown_ForAnInfrastructureFailure_AndForATurnThatErred()
    {
        // The failed turn is a whole, right answer (the tool ran for the town, the prices link to the shop): without the infrastructure check it would pass.
        var failed = Trial(CookEvalMode, "cook-x", Checkers, null,
            Turn(1, [FindsInMalmesbury, Text(CheckersList)], TurnFailure.Timeout));
        var erred = Trial(CookEvalMode, "cook-x", Checkers, null, Turn(1, [Error()]));

        var failedGrade = await new PricedAtRetailer().GradeAsync(failed, CancellationToken.None);

        Assert.That(failedGrade.Outcome, Is.EqualTo(GradeOutcome.Unknown));
        Assert.That(failedGrade.Reason, Does.Contain("infrastructure"));
        Assert.That((await new PricedAtRetailer().GradeAsync(erred, CancellationToken.None)).Outcome,
            Is.EqualTo(GradeOutcome.Unknown));
    }
}
