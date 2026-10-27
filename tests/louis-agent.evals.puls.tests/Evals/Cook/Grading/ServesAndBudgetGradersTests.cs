using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Trials;
using LouisAgent.Evals.Puls.Evals.Cook.Grading;
using static LouisAgent.Evals.Puls.Tests.PulsTrials;

namespace LouisAgent.Evals.Puls.Tests.Evals.Cook.Grading;

// F18 step 5.4: two scenario requirements, each read by a named grader. The people it serves is a fixed rule that is not required (the real
// check, that the quantities scale, is step 6's model grader); the budget is required where a scenario sets one. Written first: the graders
// threw NotImplementedException on the red run.
public class ServesAndBudgetGradersTests
{
    private const string Checkers = "https://www.checkers.co.za/p/";

    private static Trial Asked(string expected, params string[] answers) =>
        Trial(CookEvalMode, "cook-x", expected, "Stellenbosch, Wes-Kaap, Suid-Afrika",
            answers.Select((answer, i) => Turn(i + 1, [Text(answer)])).ToArray());

    private static Task<Grade> Serves(Trial trial) => new ServesAsAsked().GradeAsync(trial, CancellationToken.None).AsTask();

    private static Task<Grade> Budget(Trial trial) => new WithinBudget().GradeAsync(trial, CancellationToken.None).AsTask();

    // --- ServesAsAsked

    [Test]
    public void ServesIsAFixedRule_ThatIsNotRequired_AndReadsOneKey()
    {
        var grader = new ServesAsAsked();

        Assert.That((grader.Name, grader.Kind, grader.Required), Is.EqualTo(("serves-as-asked", GraderKind.FixedRule, false)));
        Assert.That(grader.ExpectedKeys, Is.EqualTo(new[] { "serves" }));
    }

    [TestCase("""{"serves":4}""", "Hier is bobotie vir 4 mense.")]
    [TestCase("""{"serves":4}""", "Hier is bobotie vir vier mense.")]
    [TestCase("""{"serves":2}""", "A stew for two.")]
    public async Task ServesPasses_WhenTheAnswerNamesTheNumberAsked(string expected, string answer) =>
        Assert.That((await Serves(Asked(expected, answer))).Outcome, Is.EqualTo(GradeOutcome.Pass));

    [Test]
    public async Task ServesChecksEachTurnAgainstItsOwnNumber()
    {
        var both = await Serves(Asked("""{"serves":[4,6]}""", "Vir 4 mense.", "Nou vir ses mense."));
        var forgot = await Serves(Asked("""{"serves":[4,6]}""", "Vir 4 mense.", "Hier is die nuwe lys."));

        Assert.That(both.Outcome, Is.EqualTo(GradeOutcome.Pass));
        Assert.That(forgot.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(forgot.Reason, Does.Contain("turn 2").And.Contain("6"));
    }

    [Test]
    public async Task ServesFails_NamingTheNumberItFound()
    {
        var grade = await Serves(Asked("""{"serves":4}""", "Hier is bobotie vir 2 mense."));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("2").And.Contain("4"));
    }

    [Test]
    public async Task ServesFails_WhenTheAnswerForgotTheNumber()
    {
        var grade = await Serves(Asked("""{"serves":4}""", "Hier is bobotie. Bak dit vir 45 minute."));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("4"));
    }

    [Test]
    public async Task ServesIsUnknown_WhenThereIsNothingToRead()
    {
        var erred = Trial(CookEvalMode, "cook-x", """{"serves":4}""", null, Turn(1, [Error()]));
        var blank = Asked("""{"serves":4}""", "");
        var missingTurn = Asked("""{"serves":[4,6]}""", "Vir 4 mense.");

        foreach (var trial in new[] { erred, blank, missingTurn })
            Assert.That((await Serves(trial)).Outcome, Is.EqualTo(GradeOutcome.Unknown));
    }

    [Test]
    public async Task ServesIsUnknown_ForAnInfrastructureFailure_EvenWhenTheAnswerNamesTheNumber()
    {
        // The turn has a good answer: without the infrastructure check this would pass, so the reason shows the check ran.
        var failed = Trial(CookEvalMode, "cook-x", """{"serves":4}""", null, Turn(1, [Text("Hier is bobotie vir 4 mense.")], TrialFailure.Timeout));

        var grade = await Serves(failed);

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Unknown));
        Assert.That(grade.Reason, Does.Contain("infrastructure"));
    }

    [Test]
    public async Task ServesPasses_WhenTheScenarioAsksForNoNumber() =>
        Assert.That((await Serves(Asked("{}", "Hier is 'n resep."))).Outcome, Is.EqualTo(GradeOutcome.Pass));

    // --- WithinBudget

    [Test]
    public void BudgetIsRequired_AndReadsOneKey()
    {
        var grader = new WithinBudget();

        Assert.That((grader.Name, grader.Kind, grader.Required), Is.EqualTo(("within-budget", GraderKind.FixedRule, true)));
        Assert.That(grader.ExpectedKeys, Is.EqualTo(new[] { "budget" }));
    }

    [TestCase("Totaal: R149,95")]
    [TestCase("Totaal: R150")]
    [TestCase("Total: R149.95")]
    public async Task BudgetPasses_AtOrUnderTheLimit(string line)
    {
        var grade = await Budget(Asked("""{"budget":150}""", "Hier is die lys.\n" + line));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Pass));
        Assert.That(grade.Reason, Does.Contain("150"));
    }

    [Test]
    public async Task BudgetFails_OverTheLimit_SayingWhichReadingItUsed()
    {
        var byLine = await Budget(Asked("""{"budget":150}""", "Totaal: R150,01"));
        var byRows = await Budget(Asked("""{"budget":150}""",
            "| Bestanddeel | Checkers |\n|---|---|\n| bees | R120,00 [a](" + Checkers + "1) |\n| uie | R45,00 [b](" + Checkers + "2) |\n"));

        Assert.That(byLine.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(byLine.Reason, Does.Contain("150,01").Or.Contain("150.01"));
        Assert.That(byLine.Reason, Does.Contain("total line"));
        Assert.That(byRows.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(byRows.Reason, Does.Contain("rows"));
    }

    [Test]
    public async Task BudgetIsUnknown_WhenTheAnswerGivesNoAmount()
    {
        var grade = await Budget(Asked("""{"budget":150}""", "Ek kon nie pryse vind nie."));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Unknown));
    }

    [Test]
    public async Task BudgetReadsTheFirstTurnOnly()
    {
        var grade = await Budget(Asked("""{"budget":250}""", "Totaal: R186,40", "Vir ses mense: Totaal: R999,00"));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Pass));
    }

    [Test]
    public async Task BudgetPasses_WhenTheScenarioSetsNone_AndIsUnknownForAnInfrastructureFailure()
    {
        Assert.That((await Budget(Asked("{}", "Totaal: R999,00"))).Outcome, Is.EqualTo(GradeOutcome.Pass));

        // A turn that failed but whose answer is within the budget: without the infrastructure check this would pass.
        var failed = await Budget(Trial(CookEvalMode, "cook-x", """{"budget":150}""", null, Turn(1, [Text("Totaal: R100,00")], TrialFailure.Transport)));

        Assert.That(failed.Outcome, Is.EqualTo(GradeOutcome.Unknown));
        Assert.That(failed.Reason, Does.Contain("infrastructure"));
    }
}
