using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Identities.Puls.Evals.Checks.Cook.Grading.WhereToBuy;
using LouisAgent.SessionStream;
using static LouisAgent.Identities.Puls.Tests.Evals.Checks.PulsTrials;

namespace LouisAgent.Identities.Puls.Tests.Evals.Checks.Cook.Grading.WhereToBuy;

/// <summary>
/// When the user's shop has no branch in their town (PnP in Prince Albert), the cook prices nothing, reads no shop
/// page, writes estimates under the line that says so, and says there is no branch. Louis, 2026-10-25: "make it
/// estimates, no prices, and say there's no branch".
/// </summary>
public class PricedAtNoBranchTests
{
    /// <summary>A scenario whose shop has no branch in Prince Albert.</summary>
    private const string NoBranch = """{"town":"Prince Albert","shop":"no-branch"}""";

    /// <summary>A list of estimated prices, with no shop column.</summary>
    private const string Estimates =
        "| Ingredient | For the recipe | To buy | Estimated price |\n|---|---|---|---|\n| Uie | 2 | onions 1 kg | ± R25 |\n";

    /// <summary>The Afrikaans line that says the prices are estimates.</summary>
    private const string EstimatesLine = "*Pryse is skattings, nie by 'n winkel nagegaan nie.*\n";

    /// <summary>The Afrikaans line that says there's no PnP in town.</summary>
    private const string NoBranchLine = "*Daar is geen PnP-tak in Prince Albert nie.*\n";

    /// <summary>A list with a price linked to PnP's site.</summary>
    private const string Priced =
        "| Ingredient | For the recipe | To buy | PnP |\n|---|---|---|---|\n| Uie | 2 | Uie 1 kg | [R47,00](https://www.pnp.co.za/p/1) |\n";

    /// <summary>The cook reading a PnP search page in the browser.</summary>
    private static readonly SessionEvent ReadsPnp = SpecialistCall("ReadRenderedPage",
        """{"url":"https://www.pnp.co.za/search/uie","maxChars":3000}""");

    /// <summary>The check's grade of the no-branch scenario's one turn, made of these events.</summary>
    private static Task<Grade> Grade(params SessionEvent[] events) =>
        new EstimatesAtShop().GradeAsync(Trial(CookEvalMode, "cook-no-branch", NoBranch, null, Turn(1, events)),
            CancellationToken.None).AsTask();

    /// <summary>Nothing priced, the estimates said and the missing branch said: a pass.</summary>
    [Test]
    public async Task ItPasses_WhenNothingIsPricedTheEstimatesAreSaidAndTheMissingBranchIsSaid() =>
        Assert.That((await Grade(Text(Estimates + EstimatesLine + NoBranchLine))).Outcome,
            Is.EqualTo(GradeOutcome.Pass));

    /// <summary>The same lines in English count too.</summary>
    [Test]
    public async Task TheEnglishWordsCountToo() =>
        Assert.That(
            (await Grade(Text(Estimates +
                              "*Prices are estimates, not checked at a shop.*\nThere is no PnP branch in Prince Albert.\n")))
            .Outcome,
            Is.EqualTo(GradeOutcome.Pass));

    /// <summary>Not saying there's no branch fails, saying so.</summary>
    [Test]
    public async Task ItFails_WhenTheAnswerDoesNotSayThereIsNoBranch()
    {
        var grade = await Grade(Text(Estimates + EstimatesLine));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("branch"));
    }

    /// <summary>A price linked to the shop fails, since there's no branch to price at.</summary>
    [Test]
    public async Task ItFails_WhenAPriceIsLinkedToTheShop_ThoughThereIsNoBranch() =>
        Assert.That((await Grade(Text(Priced + EstimatesLine + NoBranchLine))).Outcome, Is.EqualTo(GradeOutcome.Fail));

    /// <summary>Reading a shop's page fails, saying so.</summary>
    [Test]
    public async Task ItFails_WhenAPageOfAShopWasRead()
    {
        var grade = await Grade(ReadsPnp, Text(Estimates + EstimatesLine + NoBranchLine));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("page"));
    }

    /// <summary>Calling a shop tool fails.</summary>
    [Test]
    public async Task ItFails_WhenAShopToolWasCalled() =>
        Assert.That((await Grade(
            SpecialistCall("FindProducts", """{"shop":"checkers","area":"Prince Albert","query":"uie"}"""),
            Text(Estimates + EstimatesLine + NoBranchLine))).Outcome, Is.EqualTo(GradeOutcome.Fail));

    /// <summary>Not saying the prices are estimates fails.</summary>
    [Test]
    public async Task ItFails_WhenTheAnswerDoesNotSayTheyAreEstimates() =>
        Assert.That((await Grade(Text(Estimates + NoBranchLine))).Outcome, Is.EqualTo(GradeOutcome.Fail));
}
