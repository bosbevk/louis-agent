using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Identities.Puls.Evals.Checks.Cook.Scoring;
using static LouisAgent.Identities.Puls.Tests.Evals.Checks.PulsTrials;

namespace LouisAgent.Identities.Puls.Tests.Evals.Checks.Cook.Grading;

/// <summary>
/// The cook gives estimates (<c>± R65</c> a row, no link), so the budget is held to the estimated total, which the
/// cook's prompt writes as a bold row of the list (<c>**Total** ... **± R312**</c>).
/// </summary>
public class WithinBudgetOnEstimatesTests
{
    /// <summary>Two estimated rows, ± R112 in all.</summary>
    private const string Rows =
        "| Ingredient | For the recipe | To buy | Estimated price |\n|---|---|---|---|\n| Beef mince | 500 g | beef mince 500 g | ± R85 |\n| Rice | 300 g | rice 500 g | ± R27 |\n";

    /// <summary>The English total and per-person rows.</summary>
    private const string EnglishEnd = "| **Total** | | | **± R112** |\n| **Per person** | | | **± R28** |\n";

    /// <summary>The Afrikaans total and per-person rows.</summary>
    private const string AfrikaansEnd = "| **Totaal** | | | **± R112** |\n| **Per persoon** | | | **± R28** |\n";

    /// <summary>
    /// The cook's budget check's grade, as its eval file builds it, of a one-turn trial with this answer.
    /// </summary>
    private static Task<Grade> Budget(string expected, string answer) =>
        CookFile.Check("within-budget").GradeAsync(
            Trial(CookEvalMode, "cook-x", expected, null, Turn(1, [Text(answer)])), CancellationToken.None).AsTask();

    /// <summary>The bold total row of an estimates list is read as the total, in either language.</summary>
    [TestCase(EnglishEnd)]
    [TestCase(AfrikaansEnd)]
    public void TheTotalRowOfAnEstimatesList_IsTheTotal(string end) =>
        Assert.That(Prices.Total(Rows + end), Is.EqualTo(new BasketTotal(112m, TotalSource.ListTotalRow)));

    /// <summary>An estimated total under the budget passes.</summary>
    [Test]
    public async Task AnEstimatedTotalUnderTheBudget_Passes() =>
        Assert.That((await Budget("""{"budget":150}""", Rows + EnglishEnd)).Outcome, Is.EqualTo(GradeOutcome.Pass));

    /// <summary>An estimated total over the budget fails.</summary>
    [Test]
    public async Task AnEstimatedTotalOverTheBudget_Fails() =>
        Assert.That((await Budget("""{"budget":100}""", Rows + EnglishEnd)).Outcome, Is.EqualTo(GradeOutcome.Fail));

    // FX24 (Louis, 2026-10-26: "fix within-budget to read the estimates"): the first live cook runs gave Unknown on
    // `cook-af` and `cook-malmesbury` though each list ended in its `**Totaal** ± R323` row. The answers also said
    // "total" elsewhere, in a sentence and in the *Onseker* table, with other amounts (the budget, the saving), so the
    // line reader found several totals that disagreed and gave up. The list's own total row is the basket's total; the
    // lines are only the fallback.

    /// <summary>Other lines that say "total" with other amounts, as the live answers had.</summary>
    private const string Elsewhere =
        "Die geskatte totaal is hoër as R250.\n\n| Wat | Waarom |\n|---|---|\n| Totaal | Die estimated total is ± R112, ± R12 bo die R100-begroting. Sonder die konfyt is die totaal ± R90. |\n";

    /// <summary>The list's own total row wins over other lines that say "total".</summary>
    [Test]
    public void TheListsTotalRow_WinsOverOtherLinesThatSayTotal() =>
        Assert.That(Prices.Total(Rows + AfrikaansEnd + "\n" + Elsewhere),
            Is.EqualTo(new BasketTotal(112m, TotalSource.ListTotalRow)));

    /// <summary>Over the budget fails, naming the total row's amount, even with other totals around it.</summary>
    [Test]
    public async Task AnEstimatedTotalOverTheBudget_Fails_NamingTheRowsTotal_EvenWithOtherTotalsInTheAnswer()
    {
        var grade = await Budget("""{"budget":100}""", Rows + AfrikaansEnd + "\n" + Elsewhere);

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("112.00"));
    }

    /// <summary>Under the budget passes, even with other totals around it.</summary>
    [Test]
    public async Task AnEstimatedTotalUnderTheBudget_Passes_EvenWithOtherTotalsInTheAnswer() =>
        Assert.That((await Budget("""{"budget":150}""", Rows + AfrikaansEnd + "\n" + Elsewhere)).Outcome,
            Is.EqualTo(GradeOutcome.Pass));

    /// <summary>Two total rows that disagree give no total, as two total lines do.</summary>
    [Test]
    public void TwoTotalRowsThatDisagree_AreNoTotal_AsTwoTotalLinesAreNone() =>
        Assert.That(Prices.Total(Rows + "| **Totaal** | | | **± R112** |\n| **Totaal** | | | **± R150** |\n"), Is.Null);

    /// <summary>The per-person amount is a share of the basket, not its total.</summary>
    [Test]
    public void PerPersonIsNoTotal() =>
        Assert.That(Prices.Total(Rows + "| **Per persoon** | | | **± R28** |\n"), Is.Null,
            "the only amount is a share of the basket, not the basket");

    /// <summary>Estimates with no total row stay Unknown: rows are only added up when they're linked.</summary>
    [Test]
    public async Task EstimatesWithNoTotalRow_StayUnknown_BecauseNoRowIsSummedWithoutALink() =>
        Assert.That((await Budget("""{"budget":150}""", Rows)).Outcome, Is.EqualTo(GradeOutcome.Unknown));
}
