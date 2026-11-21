using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;
using static LouisAgent.Identities.Puls.Tests.Evals.Checks.PulsTrials;

namespace LouisAgent.Identities.Puls.Tests.Evals.Checks.Cook.Grading;

/// <summary>
/// Two things a scenario asks for, each with its own check. The people it serves is a fixed rule that isn't required
/// (whether the quantities really scale is the model judge's job); the budget is required where a scenario sets one.
/// </summary>
public class ServesAndBudgetChecksTests
{
    /// <summary>Where a Checkers product link starts.</summary>
    private const string Checkers = "https://www.checkers.co.za/p/";

    /// <summary>A cook trial in Stellenbosch with one turn per answer.</summary>
    private static Trial Asked(string expected, params string[] answers) =>
        Trial(CookEvalMode, "cook-x", expected, "Stellenbosch, Wes-Kaap, Suid-Afrika",
            answers.Select((answer, i) => Turn(i + 1, [Text(answer)])).ToArray());

    /// <summary>The cook's serving check, as its eval file builds it.</summary>
    private static readonly ICheck ServesCheck = CookFile.Check("serves-as-asked");

    /// <summary>The cook's budget check, as its eval file builds it.</summary>
    private static readonly ICheck BudgetCheck = CookFile.Check("within-budget");

    /// <summary>The serving check's grade of a trial.</summary>
    private static Task<Grade> Serves(Trial trial) => ServesCheck.GradeAsync(trial, CancellationToken.None).AsTask();

    /// <summary>The budget check's grade of a trial.</summary>
    private static Task<Grade> Budget(Trial trial) => BudgetCheck.GradeAsync(trial, CancellationToken.None).AsTask();

    // --- serves-as-asked

    /// <summary>The serving check is a fixed rule that isn't required, and reads only <c>serves</c>.</summary>
    [Test]
    public void ServesIsAFixedRule_ThatIsNotRequired_AndReadsOneKey()
    {
        var grader = ServesCheck;

        Assert.That((grader.Name, grader.Kind, grader.Required),
            Is.EqualTo(("serves-as-asked", CheckKind.FixedRule, false)));
        Assert.That(grader.ExpectedKeys, Is.EqualTo(new[] { "serves" }));
    }

    /// <summary>The answer names the number asked, as a digit or in words.</summary>
    [TestCase("""{"serves":4}""", "Hier is bobotie vir 4 mense.")]
    [TestCase("""{"serves":4}""", "Hier is bobotie vir vier mense.")]
    [TestCase("""{"serves":2}""", "A stew for two.")]
    public async Task ServesPasses_WhenTheAnswerNamesTheNumberAsked(string expected, string answer) =>
        Assert.That((await Serves(Asked(expected, answer))).Outcome, Is.EqualTo(GradeOutcome.Pass));

    /// <summary>Each turn is held to its own number, and a fail names the turn and the number it missed.</summary>
    [Test]
    public async Task ServesChecksEachTurnAgainstItsOwnNumber()
    {
        var both = await Serves(Asked("""{"serves":[4,6]}""", "Vir 4 mense.", "Nou vir ses mense."));
        var forgot = await Serves(Asked("""{"serves":[4,6]}""", "Vir 4 mense.", "Hier is die nuwe lys."));

        Assert.That(both.Outcome, Is.EqualTo(GradeOutcome.Pass));
        Assert.That(forgot.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(forgot.Reason, Does.Contain("turn 2").And.Contain("6"));
    }

    /// <summary>A different number fails, naming both what it found and what was asked.</summary>
    [Test]
    public async Task ServesFails_NamingTheNumberItFound()
    {
        var grade = await Serves(Asked("""{"serves":4}""", "Hier is bobotie vir 2 mense."));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("2").And.Contain("4"));
    }

    /// <summary>An answer that never says how many it's for fails.</summary>
    [Test]
    public async Task ServesFails_WhenTheAnswerForgotTheNumber()
    {
        var grade = await Serves(Asked("""{"serves":4}""", "Hier is bobotie. Bak dit vir 45 minute."));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("4"));
    }

    /// <summary>An error, a blank answer or a missing turn leaves nothing to read: Unknown.</summary>
    [Test]
    public async Task ServesIsUnknown_WhenThereIsNothingToRead()
    {
        var erred = Trial(CookEvalMode, "cook-x", """{"serves":4}""", null, Turn(1, [Error()]));
        var blank = Asked("""{"serves":4}""", "");
        var missingTurn = Asked("""{"serves":[4,6]}""", "Vir 4 mense.");

        foreach (var trial in new[] { erred, blank, missingTurn })
            Assert.That((await Serves(trial)).Outcome, Is.EqualTo(GradeOutcome.Unknown));
    }

    /// <summary>A turn the infrastructure broke is Unknown, even when its answer names the number.</summary>
    [Test]
    public async Task ServesIsUnknown_ForAnInfrastructureFailure_EvenWhenTheAnswerNamesTheNumber()
    {
        // The turn has a good answer: without the infrastructure check this would pass, so the reason shows the check
        // ran.
        var failed = Trial(CookEvalMode, "cook-x", """{"serves":4}""", null,
            Turn(1, [Text("Hier is bobotie vir 4 mense.")], TurnFailure.Timeout));

        var grade = await Serves(failed);

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Unknown));
        Assert.That(grade.Reason, Does.Contain("infrastructure"));
    }

    /// <summary>A scenario that asks for no number passes.</summary>
    [Test]
    public async Task ServesPasses_WhenTheScenarioAsksForNoNumber() =>
        Assert.That((await Serves(Asked("{}", "Hier is 'n resep."))).Outcome, Is.EqualTo(GradeOutcome.Pass));

    // --- within-budget

    /// <summary>The budget check is a required fixed rule that reads only <c>budget</c>.</summary>
    [Test]
    public void BudgetIsRequired_AndReadsOneKey()
    {
        var grader = BudgetCheck;

        Assert.That((grader.Name, grader.Kind, grader.Required),
            Is.EqualTo(("within-budget", CheckKind.FixedRule, true)));
        Assert.That(grader.ExpectedKeys, Is.EqualTo(new[] { "budget" }));
    }

    /// <summary>A total at or under the limit passes, and the reason names the limit.</summary>
    [TestCase("Totaal: R149,95")]
    [TestCase("Totaal: R150")]
    [TestCase("Total: R149.95")]
    public async Task BudgetPasses_AtOrUnderTheLimit(string line)
    {
        var grade = await Budget(Asked("""{"budget":150}""", "Hier is die lys.\n" + line));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Pass));
        Assert.That(grade.Reason, Does.Contain("150"));
    }

    /// <summary>
    /// Over the limit fails, whether the total is read from its line or added up from the linked price rows, and the
    /// reason names the total it read.
    /// </summary>
    [Test]
    public async Task BudgetFails_OverTheLimit_ByTheTotalLineOrTheRows()
    {
        var byLine = await Budget(Asked("""{"budget":150}""", "Totaal: R150,01"));
        var byRows = await Budget(Asked("""{"budget":150}""",
            "| Bestanddeel | Checkers |\n|---|---|\n| bees | R120,00 [a](" + Checkers + "1) |\n| uie | R45,00 [b](" +
            Checkers + "2) |\n"));

        Assert.That(byLine.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(byLine.Reason, Does.Contain("150.01"));
        Assert.That(byRows.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(byRows.Reason, Does.Contain("165.00"));
    }

    /// <summary>An answer with no amount can't be held to the budget: Unknown.</summary>
    [Test]
    public async Task BudgetIsUnknown_WhenTheAnswerGivesNoAmount()
    {
        var grade = await Budget(Asked("""{"budget":150}""", "Ek kon nie pryse vind nie."));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Unknown));
    }

    /// <summary>Only the first turn is held to the budget; a follow-up for more people may cost more.</summary>
    [Test]
    public async Task BudgetReadsTheFirstTurnOnly()
    {
        var grade = await Budget(Asked("""{"budget":250}""", "Totaal: R186,40", "Vir ses mense: Totaal: R999,00"));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Pass));
    }

    /// <summary>
    /// With no budget set it passes; a turn the infrastructure broke is Unknown, even when its total is within budget.
    /// </summary>
    [Test]
    public async Task BudgetPasses_WhenTheScenarioSetsNone_AndIsUnknownForAnInfrastructureFailure()
    {
        Assert.That((await Budget(Asked("{}", "Totaal: R999,00"))).Outcome, Is.EqualTo(GradeOutcome.Pass));

        // A turn that failed but whose answer is within the budget: without the infrastructure check this would
        // pass.
        var failed = await Budget(Trial(CookEvalMode, "cook-x", """{"budget":150}""", null,
            Turn(1, [Text("Totaal: R100,00")], TurnFailure.Transport)));

        Assert.That(failed.Outcome, Is.EqualTo(GradeOutcome.Unknown));
        Assert.That(failed.Reason, Does.Contain("infrastructure"));
    }
}
