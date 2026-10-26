using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Models;
using LouisAgent.Evals.Puls.Evals.Cook;
using LouisAgent.Evals.Puls.Evals.Cook.Grading.Judges;
using LouisAgent.Evals.Trials;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using static LouisAgent.Evals.Puls.Tests.PulsTrials;

namespace LouisAgent.Evals.Puls.Tests.Evals.Cook.Grading.Judges;

// F18 step 6.2: the cook's model grader. It judges the last answer for the people asked in that turn (the scenario's `serves`, a number for the first turn or one per turn, carried
// forward), hands the judge the ask as an EvaluationContext, and is shown and counted but not required: the outcome doesn't wait on it until its calibration passes (step 6.3).
// A scenario that asks for no number of people has no meal to judge, and costs no request. Written first: MealForPeopleGrader did not exist.
public class MealForPeopleGraderTests
{
    private static readonly MealForPeopleTests.ScriptedJudge Client = new("""{"meal": true, "reason": "a main dish for the people asked"}""");

    private static MealForPeopleGrader Grader(MealForPeopleTests.ScriptedJudge? client = null) =>
        MealForPeopleGrader.Create(() => new ConfiguredJudge(new ChatConfiguration(client ?? Client), "qwen3:8b"));

    private static Trial Cook(string expected, int turns) =>
        Trial(CookMode, "cook-x", expected, "Malmesbury", Enumerable.Range(1, turns).Select(n => Turn(n, [Text($"antwoord {n}")])).ToArray());

    [Test]
    public void ItIsAModelGrader_ThatReadsServes_AndIsNotRequired()
    {
        var grader = Grader();

        Assert.That((grader.Name, grader.Kind, grader.Required), Is.EqualTo(("meal-for-people", GraderKind.Model, false)));
        Assert.That(grader.ExpectedKeys, Is.EquivalentTo(new[] { "serves" }));
    }

    [Test]
    public async Task ItJudgesTheAnswerForThePeopleAskedAndSaysWhatJudged()
    {
        var client = new MealForPeopleTests.ScriptedJudge("""{"meal": true, "reason": "feeds four"}""");

        var grade = await Grader(client).GradeAsync(Cook("""{"serves":4,"budget":150,"town":"Malmesbury"}""", turns: 1), CancellationToken.None);

        Assert.That((grade.Outcome, grade.Reason), Is.EqualTo((GradeOutcome.Pass, "feeds four")));
        Assert.That(grade.Judge, Is.EqualTo(new GradeJudge("qwen3:8b", MealForPeoplePrompt.Hash)));
        string user = client.Sent.Single()[1].Text;
        Assert.That(user, Does.Contain("People: 4").And.Contain("Budget: R150").And.Contain("Town: Malmesbury").And.Contain("antwoord 1"));
    }

    [TestCase("""{"serves":[4,6]}""", 2, 6)]
    [TestCase("""{"serves":2}""", 2, 2)]
    [TestCase("""{"serves":[4,6]}""", 1, 4)]
    public async Task TheLastAnswersPeople_AreTheLastNumberAskedUpToItsTurn(string expected, int turns, int people)
    {
        var client = new MealForPeopleTests.ScriptedJudge("""{"meal": true, "reason": "r"}""");

        await Grader(client).GradeAsync(Cook(expected, turns), CancellationToken.None);

        string user = client.Sent.Single()[1].Text;
        Assert.That(user, Does.Contain($"People: {people}").And.Contain($"antwoord {turns}").And.Not.Contain("antwoord " + (turns - 1) + "\n"));
    }

    [Test]
    public async Task AScenarioThatAsksForNoPeople_HasNoMealToJudge_AndNoRequestIsMade()
    {
        var client = new MealForPeopleTests.ScriptedJudge("""{"meal": false, "reason": "never asked"}""");

        var grade = await Grader(client).GradeAsync(Cook("{}", turns: 1), CancellationToken.None);

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Pass));
        Assert.That(grade.Reason, Does.Contain("no number of people"));
        Assert.That(client.Sent, Is.Empty);
        Assert.That(grade.Judge, Is.Null);
    }

    [Test]
    public async Task WithNoJudgeConfigured_TheGradeIsUnknown()
    {
        var grade = await MealForPeopleGrader.Create(() => null).GradeAsync(Cook("""{"serves":4}""", turns: 1), CancellationToken.None);

        Assert.That((grade.Outcome, grade.Reason), Is.EqualTo((GradeOutcome.Unknown, "no judge configured")));
    }

    [Test]
    public async Task AJudgeThatCantTell_IsUnknown_NeverAPass()
    {
        var client = new MealForPeopleTests.ScriptedJudge("""{"meal": null, "reason": "no quantities"}""");

        var grade = await Grader(client).GradeAsync(Cook("""{"serves":4}""", turns: 1), CancellationToken.None);

        Assert.That((grade.Outcome, grade.Reason), Is.EqualTo((GradeOutcome.Unknown, "no quantities")));
    }

    [Test]
    public void TheCookEvalHasIt_AfterTheFixedRules_AndTheOutcomeDoesNotWaitOnIt()
    {
        var eval = new CookEval();

        var grader = eval.Graders.Single(g => g.Name == "meal-for-people");

        Assert.That((grader.Kind, grader.Required), Is.EqualTo((GraderKind.Model, false)));
        Assert.That(eval.Graders.Where(g => g.Required).Select(g => g.Kind), Has.All.EqualTo(GraderKind.FixedRule));
        Assert.That(eval.Graders.Last(), Is.SameAs(grader));
    }
}
