using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Trials;
using LouisAgent.Evals.Puls.Evals.Cook.Grading;
using LouisAgent.Evals.Puls.Evals.Cook.Scoring;
using static LouisAgent.Evals.Puls.Tests.PulsTrials;

namespace LouisAgent.Evals.Puls.Tests.Evals.Cook.Grading;

// F18 step 5.7: the cook now gives estimates (`± R65` a row, no link), so the budget is checked against the estimated total. The cook's prompt writes the total
// as a bold row of the list (`**Total** ... **± R312**`). Written first, to see what Prices.Total reads from such an answer; the finding is in the feature doc.
public class WithinBudgetOnEstimatesTests
{
    private const string Rows = "| Ingredient | For the recipe | To buy | Estimated price |\n|---|---|---|---|\n| Beef mince | 500 g | beef mince 500 g | ± R85 |\n| Rice | 300 g | rice 500 g | ± R27 |\n";
    private const string EnglishEnd = "| **Total** | | | **± R112** |\n| **Per person** | | | **± R28** |\n";
    private const string AfrikaansEnd = "| **Totaal** | | | **± R112** |\n| **Per persoon** | | | **± R28** |\n";

    private static Task<Grade> Budget(string expected, string answer) =>
        new WithinBudget().GradeAsync(Trial(CookMode, "cook-x", expected, null, Turn(1, [Text(answer)])), CancellationToken.None).AsTask();

    [TestCase(EnglishEnd)]
    [TestCase(AfrikaansEnd)]
    public void TheTotalRowOfAnEstimatesList_IsTheTotal(string end) =>
        Assert.That(Prices.Total(Rows + end), Is.EqualTo(new BasketTotal(112m, TotalSource.TotalLine)));

    [Test]
    public async Task AnEstimatedTotalUnderTheBudget_Passes() =>
        Assert.That((await Budget("""{"budget":150}""", Rows + EnglishEnd)).Outcome, Is.EqualTo(GradeOutcome.Pass));

    [Test]
    public async Task AnEstimatedTotalOverTheBudget_Fails() =>
        Assert.That((await Budget("""{"budget":100}""", Rows + EnglishEnd)).Outcome, Is.EqualTo(GradeOutcome.Fail));

    [Test]
    public async Task EstimatesWithNoTotalRow_StayUnknown_BecauseNoRowIsSummedWithoutALink() =>
        Assert.That((await Budget("""{"budget":150}""", Rows)).Outcome, Is.EqualTo(GradeOutcome.Unknown));
}
