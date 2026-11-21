using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Packs.WhereToBuy.Checks;
using LouisAgent.SessionStream;
using static LouisAgent.Packs.WhereToBuy.Tests.Checks.ShopTrials;

namespace LouisAgent.Packs.WhereToBuy.Tests.Checks;

/// <summary>
/// A scenario that names a shop passes when the answer reads no shop page, links no price, says the
/// prices are estimates, and names the shop's branch under Where to buy. Louis, 2026-10-25: "keep cook but just rework
/// it to not lookup prices but just focus on local recipes and where to shop". <c>no-branch</c> keeps its own rule
/// (<c>EstimatesAtNoBranchTests</c>). Whether a price is given as fact is the reader's to say (a fake here; Puls's
/// <c>rand-prices</c> is tested beside Puls, and the two together by the cook's <c>CookGradesTests</c>).
/// </summary>
public class EstimatesAtShopTests
{
    /// <summary>A scenario at Malmesbury's PnP.</summary>
    private const string Pnp = """{"town":"Malmesbury","shop":"pnp"}""";

    /// <summary>A scenario at Stellenbosch's Woolworths.</summary>
    private const string Woolworths = """{"town":"Stellenbosch","shop":"woolworths"}""";

    /// <summary>A scenario at Malmesbury's Checkers.</summary>
    private const string Checkers = """{"town":"Malmesbury","shop":"checkers"}""";

    /// <summary>A list of estimated prices, with no shop column.</summary>
    private const string Estimates =
        "| Ingredient | For the recipe | To buy | Estimated price |\n|---|---|---|---|\n| Uie | 2 | onions 1 kg | ± R25 |\n";

    /// <summary>The line that says the prices are estimates.</summary>
    private const string EstimatesLine = "*Prices are estimates, not checked at a shop.*\n";

    /// <summary>A Where to buy section naming PnP's Malmesbury branch.</summary>
    private const string PnpBranch = "\n### Where to buy\n\nPick n Pay Malmesbury, Voortrekker Street\n";

    /// <summary>The Afrikaans section naming Woolworths' Stellenbosch branch.</summary>
    private const string WoolworthsBranch = "\n### Waar om te koop\n\nWoolworths Eikestad Mall, Stellenbosch\n";

    /// <summary>The cook reading a PnP search page in the browser.</summary>
    private static readonly SessionEvent ReadsPnp = SpecialistCall("ReadRenderedPage",
        """{"url":"https://www.pnp.co.za/search/uie","maxChars":3000}""");

    /// <summary>The cook fetching a PnP search page.</summary>
    private static readonly SessionEvent FetchesPnp =
        SpecialistCall("FetchUrl", """{"url":"https://www.pnp.co.za/search/uie"}""");

    /// <summary>The cook reading a recipe page, which is fine.</summary>
    private static readonly SessionEvent ReadsRecipe =
        SpecialistCall("FetchUrl", """{"url":"https://marindakook.co.za/bobotie/"}""");

    /// <summary>
    /// The check's grade of a one-turn trial made of these events, with a reader that finds no price.
    /// </summary>
    private static Task<Grade> Grade(string expected, params SessionEvent[] events) =>
        Grade(FakePrices.None, expected, events);

    /// <summary>The check's grade of a one-turn trial made of these events, over this price reader.</summary>
    private static Task<Grade> Grade(FakePrices prices, string expected, params SessionEvent[] events) =>
        new EstimatesAtShop(prices).GradeAsync(Trial(expected, Turn(1, events)), CancellationToken.None).AsTask();

    /// <summary>It's a required fixed rule, at version 3, that reads the shop and the town.</summary>
    [Test]
    public void ItIsARequiredFixedRule_ThatReadsTheShopAndTheTownKeys()
    {
        var grader = new EstimatesAtShop(FakePrices.None);

        Assert.That((grader.Name, grader.Version, grader.Kind, grader.Required),
            Is.EqualTo(("estimates-at-shop", 3, CheckKind.FixedRule, true)));
        Assert.That(grader.ExpectedKeys, Is.EquivalentTo(new[] { "shop", "town" }));
    }

    /// <summary>Estimates said, no shop page read, no price linked, and the branch named: a pass.</summary>
    [Test]
    public async Task PnP_Passes_WhenNoShopPageWasReadNoPriceIsLinkedTheEstimatesAreSaidAndTheBranchIsNamed() =>
        Assert.That((await Grade(Pnp, ReadsRecipe, Text(Estimates + EstimatesLine + PnpBranch))).Outcome,
            Is.EqualTo(GradeOutcome.Pass));

    /// <summary>The Afrikaans line and heading count as the English ones do.</summary>
    [Test]
    public async Task Woolworths_Passes_WithTheAfrikaansLinesAndHeading() =>
        Assert.That(
            (await Grade(Woolworths,
                Text(Estimates + "*Pryse is skattings, nie by 'n winkel nagegaan nie.*\n" + WoolworthsBranch))).Outcome,
            Is.EqualTo(GradeOutcome.Pass));

    /// <summary>Checkers is held to the same rule, since no shop is priced now.</summary>
    [Test]
    public async Task Checkers_Passes_TheSameWay_BecauseNoShopIsPricedNow() =>
        Assert.That(
            (await Grade(Checkers, Text(Estimates + EstimatesLine + "\n### Where to buy\n\nCheckers Malmesbury\n")))
            .Outcome, Is.EqualTo(GradeOutcome.Pass));

    /// <summary>Reading a shop's page in the browser fails, saying so.</summary>
    [Test]
    public async Task ItFails_WhenAShopPageWasReadWithReadRenderedPage()
    {
        var grade = await Grade(Pnp, ReadsPnp, Text(Estimates + EstimatesLine + PnpBranch));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("page"));
    }

    /// <summary>Fetching a shop's page fails too.</summary>
    [Test]
    public async Task ItFails_WhenAShopPageWasFetchedWithFetchUrl() =>
        Assert.That((await Grade(Pnp, FetchesPnp, Text(Estimates + EstimatesLine + PnpBranch))).Outcome,
            Is.EqualTo(GradeOutcome.Fail));

    /// <summary>The map and the price table read no shop, so using them passes.</summary>
    [Test]
    public async Task ItPasses_WhenTheMapAndThePriceTableWereUsed() =>
        Assert.That((await Grade(Pnp,
            SpecialistCall("FindBranches", """{"shop":"Pick n Pay","town":"Malmesbury"}"""),
            SpecialistCall("EstimatePrices", """{"items":["onions 1 kg"],"province":"Western Cape"}"""),
            Text(Estimates + EstimatesLine + PnpBranch))).Outcome, Is.EqualTo(GradeOutcome.Pass));

    /// <summary>A price linked to a shop, as the reader reads it, fails, saying so.</summary>
    [Test]
    public async Task ItFails_WhenAPriceLinksToAShop()
    {
        var grade = await Grade(new FakePrices(0, "pnp.co.za"), Pnp, Text(Estimates + EstimatesLine + PnpBranch));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("price"));
    }

    /// <summary>A price the reader reads as given as fact, with no link, fails too.</summary>
    [Test]
    public async Task ItFails_WhenAPriceIsGivenAsFact() =>
        Assert.That((await Grade(new FakePrices(1), Pnp, Text(Estimates + EstimatesLine + PnpBranch))).Outcome,
            Is.EqualTo(GradeOutcome.Fail));

    /// <summary>Without the line saying they're estimates, it fails.</summary>
    [Test]
    public async Task ItFails_WhenTheEstimatesLineIsMissing()
    {
        var grade = await Grade(Pnp, Text(Estimates + PnpBranch));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("estimates"));
    }

    /// <summary>A Where to buy section that names no branch fails.</summary>
    [Test]
    public async Task ItFails_WhenNoBranchIsNamed()
    {
        var grade = await Grade(Pnp,
            Text(Estimates + EstimatesLine + "\n### Where to buy\n\nAny supermarket near you.\n"));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("branch"));
    }

    /// <summary>A turn the infrastructure broke is Unknown, even with a good answer.</summary>
    [Test]
    public async Task ItIsUnknown_ForAnInfrastructureFailure()
    {
        var failed = Trial(Pnp, Turn(1, [Text(Estimates + EstimatesLine + PnpBranch)], TurnFailure.Timeout));

        Assert.That((await new EstimatesAtShop(FakePrices.None).GradeAsync(failed, CancellationToken.None)).Outcome,
            Is.EqualTo(GradeOutcome.Unknown));
    }

    // Version 2 (review of 0ea184ef1953): a fail says which case it is, so it can be told apart without the answer,
    // and the branch is read the way a person reads it.

    /// <summary>With no Where to buy section at all, the fail says just that.</summary>
    [Test]
    public async Task ItFails_SayingThereIsNoWhereToBuySection_WhenThereIsNone()
    {
        var grade = await Grade(Pnp, ReadsRecipe,
            Text(Estimates + EstimatesLine + "\n### Recipe\n\nPick n Pay Malmesbury\n"));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("no Where to buy section"));
    }

    /// <summary>A section that names no branch with a place fails, saying so.</summary>
    [Test]
    public async Task ItFails_SayingNoBranchOfTheShopWithAPlace_WhenTheSectionNamesNone()
    {
        var grade = await Grade(Pnp, ReadsRecipe,
            Text(Estimates + EstimatesLine + "\n### Where to buy\n\nAny supermarket near you.\n"));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("no pnp branch with a place"));
    }

    /// <summary>"The nearest branch" without a place is not a branch, and the fail says so.</summary>
    [Test]
    public async Task ItFails_SayingOnlyANearestBranch_WhenThatIsAllItGives()
    {
        var grade = await Grade(Pnp, ReadsRecipe, Text(Estimates + EstimatesLine
                                                                 + "\n### Where to buy\n\n| Shop | Branch | Street or mall |\n|---|---|---|\n| Pick n Pay | nearest Pick n Pay | |\n"));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("only a nearest pnp branch"));
    }

    /// <summary>A bold heading instead of a markdown one, with the branch in a table, passes.</summary>
    [Test]
    public async Task ItPasses_WhenTheTableOfAPlainBoldHeadingNamesTheBranch() =>
        Assert.That((await Grade(Pnp, ReadsRecipe, Text(Estimates + EstimatesLine
                                                                  + "\n**Where to buy**\n\n| Shop | Branch | Street or mall |\n|---|---|---|\n| PnP | Pick n Pay Menlyn | Menlyn Park |\n")))
            .Outcome, Is.EqualTo(GradeOutcome.Pass));

    /// <summary>The cook asking the map for the shops near the user.</summary>
    private static readonly SessionEvent LooksUpShops =
        SpecialistCall("FindShopsNear", """{"town":"Stellenbosch"}""");

    /// <summary>A user in a town who named no shop, or none they mind.</summary>
    private const string AnyInTown = """{"town":"Stellenbosch","shop":"any"}""";

    /// <summary>The same with no town in the scenario: there's nothing to look shops up near.</summary>
    private const string AnyNoTown = """{"shop":"any"}""";

    /// <summary>The old line, which sends the user off to look for themselves.</summary>
    private const string AnySupermarket = "\n### Where to buy\n\nAny supermarket near you.\n";

    /// <summary>A shop near the user, with its place.</summary>
    private const string ShopNearby =
        "\n### Where to buy\n\n| Shop | Branch | Street or mall |\n|---|---|---|\n| Spar | Eikestad Spar | Andringa Street |\n";

    /// <summary>Knowing the user's town, the old line is a fail: this is the bug the check let through.</summary>
    [Test]
    public async Task AnyShop_InATown_FailsTheOldLine()
    {
        var grade = await Grade(AnyInTown, ReadsRecipe, LooksUpShops, Text(Estimates + EstimatesLine + AnySupermarket));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("names none near them"));
    }

    /// <summary>Knowing the user's town, a shop near them with its place, from the map, passes.</summary>
    [Test]
    public async Task AnyShop_InATown_PassesAShopNearby() =>
        Assert.That(
            (await Grade(AnyInTown, ReadsRecipe, LooksUpShops, Text(Estimates + EstimatesLine + ShopNearby))).Outcome,
            Is.EqualTo(GradeOutcome.Pass));

    /// <summary>A shop named with no look-up came from the model's memory, and it may not be there at all.</summary>
    [Test]
    public async Task AnyShop_InATown_FailsAShopNamedWithNoLookup()
    {
        var grade = await Grade(AnyInTown, ReadsRecipe, Text(Estimates + EstimatesLine + ShopNearby));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("FindShopsNear"));
    }

    /// <summary>With no town to look up, there's nothing to call and no shop to name.</summary>
    [Test]
    public async Task AnyShop_WithNoTown_NeedsNoLookup() =>
        Assert.That((await Grade(AnyNoTown, ReadsRecipe, Text(Estimates + EstimatesLine + AnySupermarket))).Outcome,
            Is.EqualTo(GradeOutcome.Pass));
}
