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
    private const string Rows =
        "| Ingredient | For the recipe | To buy | Estimated price |\n|---|---|---|---|\n| Beef mince | 500 g | beef mince 500 g | ± R85 |\n| Rice | 300 g | rice 500 g | ± R27 |\n";

    private const string EnglishEnd = "| **Total** | | | **± R112** |\n| **Per person** | | | **± R28** |\n";
    private const string AfrikaansEnd = "| **Totaal** | | | **± R112** |\n| **Per persoon** | | | **± R28** |\n";

    private static Task<Grade> Budget(string expected, string answer) =>
        new WithinBudget().GradeAsync(Trial(CookEvalMode, "cook-x", expected, null, Turn(1, [Text(answer)])),
            CancellationToken.None).AsTask();

    [TestCase(EnglishEnd)]
    [TestCase(AfrikaansEnd)]
    public void TheTotalRowOfAnEstimatesList_IsTheTotal(string end) =>
        Assert.That(Prices.Total(Rows + end), Is.EqualTo(new BasketTotal(112m, TotalSource.ListTotalRow)));

    [Test]
    public async Task AnEstimatedTotalUnderTheBudget_Passes() =>
        Assert.That((await Budget("""{"budget":150}""", Rows + EnglishEnd)).Outcome, Is.EqualTo(GradeOutcome.Pass));

    [Test]
    public async Task AnEstimatedTotalOverTheBudget_Fails() =>
        Assert.That((await Budget("""{"budget":100}""", Rows + EnglishEnd)).Outcome, Is.EqualTo(GradeOutcome.Fail));

    // FX24 (Louis, 2026-10-26: "fix within-budget to read the estimates"): the first live cook runs gave Unknown on `cook-af` and `cook-malmesbury` though each list ended in its
    // `**Totaal** ± R323` row. The answers also said "total" elsewhere, in a sentence and in the *Onseker* table, with other amounts (the budget, the saving), so the line reader
    // found several totals that disagreed and gave up. The list's own total row is the basket's total; the lines are only the fallback.
    private const string Elsewhere =
        "Die geskatte totaal is hoër as R250.\n\n| Wat | Waarom |\n|---|---|\n| Totaal | Die estimated total is ± R112, ± R12 bo die R100-begroting. Sonder die konfyt is die totaal ± R90. |\n";

    [Test]
    public void TheListsTotalRow_WinsOverOtherLinesThatSayTotal() =>
        Assert.That(Prices.Total(Rows + AfrikaansEnd + "\n" + Elsewhere),
            Is.EqualTo(new BasketTotal(112m, TotalSource.ListTotalRow)));

    [Test]
    public async Task AnEstimatedTotalOverTheBudget_Fails_NamingTheRowItWasReadFrom_EvenWithOtherTotalsInTheAnswer()
    {
        var grade = await Budget("""{"budget":100}""", Rows + AfrikaansEnd + "\n" + Elsewhere);

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("112").And.Contain("total row"));
    }

    [Test]
    public async Task AnEstimatedTotalUnderTheBudget_Passes_EvenWithOtherTotalsInTheAnswer() =>
        Assert.That((await Budget("""{"budget":150}""", Rows + AfrikaansEnd + "\n" + Elsewhere)).Outcome,
            Is.EqualTo(GradeOutcome.Pass));

    [Test]
    public void TwoTotalRowsThatDisagree_AreNoTotal_AsTwoTotalLinesAreNone() =>
        Assert.That(Prices.Total(Rows + "| **Totaal** | | | **± R112** |\n| **Totaal** | | | **± R150** |\n"), Is.Null);

    [Test]
    public void PerPersonIsNoTotal() =>
        Assert.That(Prices.Total(Rows + "| **Per persoon** | | | **± R28** |\n"), Is.Null,
            "the only amount is a share of the basket, not the basket");

    [Test]
    public async Task EstimatesWithNoTotalRow_StayUnknown_BecauseNoRowIsSummedWithoutALink() =>
        Assert.That((await Budget("""{"budget":150}""", Rows)).Outcome, Is.EqualTo(GradeOutcome.Unknown));
}
