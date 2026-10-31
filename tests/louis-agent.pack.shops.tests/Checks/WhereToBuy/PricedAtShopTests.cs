using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Packs.Shops.Checks;
using LouisAgent.SessionStream;
using static LouisAgent.Packs.Shops.Tests.Checks.ShopTrials;

namespace LouisAgent.Packs.Shops.Tests.Checks.WhereToBuy;

/// <summary>
/// The shop the user chose, held over the cook's calls and its answer. A user's shop (Checkers) is priced with its
/// tool, with the user's town as the <c>area</c> in every call, and the prices link to its site; when the user chose
/// none (<c>any</c>), the cook prices nothing, reads no shop, and writes estimates (<c>± R65</c>, no link) under a
/// line that says so. Which sites the prices link to is the reader's to say: each case's fake reads what Puls's
/// <c>rand-prices</c> reads in its list (tested beside Puls).
/// </summary>
public class PricedAtShopTests
{
    /// <summary>A scenario at Malmesbury's Checkers.</summary>
    private const string Checkers = """{"town":"Malmesbury","shop":"checkers"}""";

    /// <summary>A scenario in Stellenbosch where any shop will do.</summary>
    private const string Any = """{"town":"Stellenbosch","shop":"any"}""";

    /// <summary>The cook searching Checkers in Malmesbury.</summary>
    private static readonly SessionEvent FindsInMalmesbury =
        SpecialistCall("FindProducts", """{"shop":"checkers","area":"Malmesbury","query":"uie"}""");

    /// <summary>A list priced at Checkers, its price linked to Checkers' site.</summary>
    private const string CheckersList =
        "| Ingredient | For the recipe | To buy | Checkers |\n|---|---|---|---|\n| Uie | 2 | Uie 1 kg | [R45,99](https://www.checkers.co.za/p/1) |\n";

    /// <summary>A list priced at Pick n Pay, its price linked to PnP's site.</summary>
    private const string PnpList =
        "| Ingredient | For the recipe | To buy | Pick n Pay |\n|---|---|---|---|\n| Uie | 2 | Uie 1 kg | [R47,00](https://www.pnp.co.za/p/1) |\n";

    /// <summary>A list of estimated prices, with no shop column.</summary>
    private const string EstimateList =
        "| Ingredient | For the recipe | To buy | Estimated price |\n|---|---|---|---|\n| Uie | 2 | onions 1 kg | ± R25 |\n";

    /// <summary>The Afrikaans line that says the prices are estimates.</summary>
    private const string EstimatesLine = "*Pryse is skattings, nie by 'n winkel nagegaan nie.*";

    /// <summary>What Puls's reader reads in the Checkers list: one price as fact, linked to Checkers.</summary>
    private static readonly FakePrices AtCheckers = new(1, "checkers.co.za");

    /// <summary>What Puls's reader reads in the PnP list: one price as fact, linked to PnP.</summary>
    private static readonly FakePrices AtPnp = new(1, "pnp.co.za");

    /// <summary>The estimates check's grade of a one-turn trial made of these events, over this reader.</summary>
    private static Task<Grade> Estimate(FakePrices prices, string expected, params SessionEvent[] events) =>
        new EstimatesAtShop(prices).GradeAsync(Trial(expected, Turn(1, events)), CancellationToken.None).AsTask();

    /// <summary>The priced-at-shop check's grade of a one-turn trial made of these events, over this reader.</summary>
    private static Task<Grade> Grade(FakePrices prices, string expected, params SessionEvent[] events) =>
        new PricedAtRetailer(prices).GradeAsync(Trial(expected, Turn(1, events)), CancellationToken.None).AsTask();

    /// <summary>It's a required fixed rule that reads the shop and the town.</summary>
    [Test]
    public void ItIsARequiredFixedRule_ThatReadsTheShopAndTheTownKeys()
    {
        var grader = new PricedAtRetailer(FakePrices.None);

        Assert.That((grader.Name, grader.Kind, grader.Required),
            Is.EqualTo(("priced-at-shop", CheckKind.FixedRule, true)));
        Assert.That(grader.ExpectedKeys, Is.EquivalentTo(new[] { "shop", "town" }));
    }

    // --- the user's shop

    /// <summary>The shop's tool ran for the town and the prices link to the shop: a pass.</summary>
    [Test]
    public async Task WithAShop_ItPasses_WhenTheShopToolRanForTheTownAndThePricesLinkToTheShop() =>
        Assert.That((await Grade(AtCheckers, Checkers, FindsInMalmesbury, Text(CheckersList))).Outcome,
            Is.EqualTo(GradeOutcome.Pass));

    /// <summary>The area matches the town whatever its case, and with the province after it.</summary>
    [Test]
    public async Task WithAShop_TheAreaMatchesTheTownWhateverTheCaseOrExtraWords()
    {
        var found = SpecialistCall("SearchSpecials", """{"shop":"Checkers","area":"malmesbury, western cape"}""");

        Assert.That((await Grade(AtCheckers, Checkers, found, Text(CheckersList))).Outcome,
            Is.EqualTo(GradeOutcome.Pass));
    }

    /// <summary>Prices without the shop's tool ever called fail, naming the shop.</summary>
    [Test]
    public async Task WithAShop_ItFails_WhenTheToolWasNeverCalled()
    {
        var grade = await Grade(AtCheckers, Checkers, Text(CheckersList));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("checkers"));
    }

    /// <summary>A call for another town fails, naming the user's town.</summary>
    [Test]
    public async Task WithAShop_ItFails_WhenACallNamesAnotherTown()
    {
        var elsewhere = SpecialistCall("FindProducts", """{"shop":"checkers","area":"Stellenbosch","query":"uie"}""");

        var grade = await Grade(AtCheckers, Checkers, elsewhere, Text(CheckersList));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("Malmesbury"));
    }

    /// <summary>A call that leaves the area out fails: the cook's prompt says to give it in every call.</summary>
    [Test]
    public async Task WithAShop_ItFails_WhenACallLeavesTheAreaOut_BecauseTheCookPromptSaysAreaInEveryCall()
    {
        var noArea = SpecialistCall("FindProducts", """{"shop":"checkers","query":"uie"}""");

        Assert.That((await Grade(AtCheckers, Checkers, FindsInMalmesbury, noArea, Text(CheckersList))).Outcome,
            Is.EqualTo(GradeOutcome.Fail));
    }

    /// <summary>Prices that link to another shop fail.</summary>
    [Test]
    public async Task WithAShop_ItFails_WhenThePricesLinkToAnotherShop() =>
        Assert.That((await Grade(AtPnp, Checkers, FindsInMalmesbury, Text(PnpList))).Outcome,
            Is.EqualTo(GradeOutcome.Fail));

    /// <summary>An answer with no priced list fails.</summary>
    [Test]
    public async Task WithAShop_ItFails_WhenThereIsNoPricedList() =>
        Assert.That(
            (await Grade(FakePrices.None, Checkers, FindsInMalmesbury, Text("Ek weet nie wat dit kos nie."))).Outcome,
            Is.EqualTo(GradeOutcome.Fail));

    // --- no shop chosen

    /// <summary>With any shop: nothing searched, no price linked, and the estimates said is a pass.</summary>
    [Test]
    public async Task WithAnyShop_ItPasses_WhenNothingWasSearchedNoPriceIsLinkedAndTheAnswerSaysTheyAreEstimates() =>
        Assert.That((await Estimate(FakePrices.None, Any, Text(EstimateList + EstimatesLine))).Outcome,
            Is.EqualTo(GradeOutcome.Pass));

    /// <summary>The estimates line in English counts too.</summary>
    [Test]
    public async Task WithAnyShop_TheEnglishLineCountsToo() =>
        Assert.That(
            (await Estimate(FakePrices.None, Any, Text(EstimateList + "Prices are estimates, not checked at a shop.")))
            .Outcome, Is.EqualTo(GradeOutcome.Pass));

    /// <summary>With any shop, calling a shop tool fails, saying so.</summary>
    [Test]
    public async Task WithAnyShop_ItFails_WhenAShopToolWasCalled()
    {
        var grade = await Estimate(FakePrices.None, Any, FindsInMalmesbury, Text(EstimateList + EstimatesLine));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("shop tool"));
    }

    /// <summary>With any shop, a price linked to a shop fails, saying so.</summary>
    [Test]
    public async Task WithAnyShop_ItFails_WhenAPriceIsLinkedToAShop()
    {
        // A price from a shop for a user who chose none: what the old wrong_store check caught.
        var grade = await Estimate(AtCheckers, Any, Text(CheckersList + EstimatesLine));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("price"));
    }

    /// <summary>A price given as fact, without the ± of an estimate, fails.</summary>
    [Test]
    public async Task WithAnyShop_ItFails_WhenAPriceIsGivenAsFact_NotAsAnEstimate() =>
        Assert.That(
            (await Estimate(new FakePrices(1), Any,
                Text(
                    "| Ingredient | For the recipe | To buy | Prys |\n|---|---|---|---|\n| Uie | 2 | uie | R20,00 |\n" +
                    EstimatesLine))).Outcome,
            Is.EqualTo(GradeOutcome.Fail));

    /// <summary>Not saying the prices are estimates fails, saying so.</summary>
    [Test]
    public async Task WithAnyShop_ItFails_WhenTheAnswerDoesNotSayTheyAreEstimates()
    {
        var grade = await Estimate(FakePrices.None, Any, Text(EstimateList));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("estimates"));
    }

    // --- every other case

    /// <summary>A scenario that names no shop passes.</summary>
    [Test]
    public async Task ItPasses_WhenTheScenarioNamesNoShop() =>
        Assert.That((await Grade(FakePrices.None, """{"routes_to":null}""", Text("Resep."))).Outcome,
            Is.EqualTo(GradeOutcome.Pass));

    /// <summary>A turn the infrastructure broke, or one that errored, is Unknown.</summary>
    [Test]
    public async Task ItIsUnknown_ForAnInfrastructureFailure_AndForATurnThatErred()
    {
        // The failed turn is a whole, right answer (the tool ran for the town, the prices link to the shop): without
        // the infrastructure check it would pass.
        var failed = Trial(Checkers, Turn(1, [FindsInMalmesbury, Text(CheckersList)], TurnFailure.Timeout));
        var erred = Trial(Checkers, Turn(1, [Error()]));

        var failedGrade = await new PricedAtRetailer(AtCheckers).GradeAsync(failed, CancellationToken.None);

        Assert.That(failedGrade.Outcome, Is.EqualTo(GradeOutcome.Unknown));
        Assert.That(failedGrade.Reason, Does.Contain("infrastructure"));
        Assert.That((await new PricedAtRetailer(AtCheckers).GradeAsync(erred, CancellationToken.None)).Outcome,
            Is.EqualTo(GradeOutcome.Unknown));
    }
}
