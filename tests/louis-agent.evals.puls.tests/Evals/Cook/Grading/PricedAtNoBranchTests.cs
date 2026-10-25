using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Trials;
using LouisAgent.SessionStream;
using LouisAgent.Evals.Puls.Evals.Cook.Grading;
using static LouisAgent.Evals.Puls.Tests.PulsTrials;

namespace LouisAgent.Evals.Puls.Tests.Evals.Cook.Grading;

// The user's shop has no branch in their town (cook-no-branch: PnP in Prince Albert): the cook prices nothing, reads no shop page, writes estimates under the
// line that says so, and says there is no branch. Louis, 2026-10-25: "make it estimates, no prices, and say there's no branch". Written first: the scenario expected
// the opposite (price at the nearest store and say so under Unsure), and `no-branch` was no value of the shop.
public class PricedAtNoBranchTests
{
    private const string NoBranch = """{"town":"Prince Albert","shop":"no-branch"}""";

    private const string Estimates = "| Ingredient | For the recipe | To buy | Estimated price |\n|---|---|---|---|\n| Uie | 2 | onions 1 kg | ± R25 |\n";
    private const string EstimatesLine = "*Pryse is skattings, nie by 'n winkel nagegaan nie.*\n";
    private const string NoBranchLine = "*Daar is geen PnP-tak in Prince Albert nie.*\n";
    private const string Priced = "| Ingredient | For the recipe | To buy | PnP |\n|---|---|---|---|\n| Uie | 2 | Uie 1 kg | [R47,00](https://www.pnp.co.za/p/1) |\n";

    private static readonly SessionEvent ReadsPnp = SpecialistCall("ReadRenderedPage", """{"url":"https://www.pnp.co.za/search/uie","maxChars":3000}""");

    private static Task<Grade> Grade(params SessionEvent[] events) =>
        new EstimatesAtShop().GradeAsync(Trial(CookMode, "cook-no-branch", NoBranch, null, Turn(1, events)), CancellationToken.None).AsTask();

    [Test]
    public async Task ItPasses_WhenNothingIsPricedTheEstimatesAreSaidAndTheMissingBranchIsSaid() =>
        Assert.That((await Grade(Text(Estimates + EstimatesLine + NoBranchLine))).Outcome, Is.EqualTo(GradeOutcome.Pass));

    [Test]
    public async Task TheEnglishWordsCountToo() =>
        Assert.That((await Grade(Text(Estimates + "*Prices are estimates, not checked at a shop.*\nThere is no PnP branch in Prince Albert.\n"))).Outcome,
            Is.EqualTo(GradeOutcome.Pass));

    [Test]
    public async Task ItFails_WhenTheAnswerDoesNotSayThereIsNoBranch()
    {
        var grade = await Grade(Text(Estimates + EstimatesLine));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("branch"));
    }

    [Test]
    public async Task ItFails_WhenAPriceIsLinkedToTheShop_ThoughThereIsNoBranch() =>
        Assert.That((await Grade(Text(Priced + EstimatesLine + NoBranchLine))).Outcome, Is.EqualTo(GradeOutcome.Fail));

    [Test]
    public async Task ItFails_WhenAPageOfAShopWasRead()
    {
        var grade = await Grade(ReadsPnp, Text(Estimates + EstimatesLine + NoBranchLine));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("page"));
    }

    [Test]
    public async Task ItFails_WhenAShopToolWasCalled() =>
        Assert.That((await Grade(SpecialistCall("FindProducts", """{"shop":"checkers","area":"Prince Albert","query":"uie"}"""),
            Text(Estimates + EstimatesLine + NoBranchLine))).Outcome, Is.EqualTo(GradeOutcome.Fail));

    [Test]
    public async Task ItFails_WhenTheAnswerDoesNotSayTheyAreEstimates() =>
        Assert.That((await Grade(Text(Estimates + NoBranchLine))).Outcome, Is.EqualTo(GradeOutcome.Fail));
}
