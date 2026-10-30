using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Grading.Models;
using LouisAgent.Evals.Puls.Evals.Cook.Grading.Judges;
using LouisAgent.Evals.Trials;
using Microsoft.Extensions.AI.Evaluation;
using static LouisAgent.Evals.Puls.Tests.PulsTrials;

namespace LouisAgent.Evals.Puls.Tests.Evals.Cook.Grading.Judges;

/// <summary>
/// The cook's model check. It judges the last answer for the people asked in that turn (the scenario's <c>serves</c>,
/// a number for the first turn or one per turn, carried forward), hands the judge the ask as an
/// <see cref="EvaluationContext"/>, and is shown and counted but not required: the outcome doesn't wait on it until
/// its calibration passes. A scenario that asks for no number of people has no meal to judge, and costs no request.
/// </summary>
public class MealForPeopleCheckTests
{
    /// <summary>A judge that always says yes.</summary>
    private static readonly MealForPeopleTests.ScriptedJudge Client =
        new("""{"meal": true, "reason": "a main dish for the people asked"}""");

    /// <summary>The cook's judge over its rubric, asking this client.</summary>
    private static MealForPeopleCheck Grader(MealForPeopleTests.ScriptedJudge? client = null) =>
        MealForPeopleCheck.Create(CookFile.Rubric,
            () => new ConfiguredJudge(new ChatConfiguration(client ?? Client), "qwen3:8b"));

    /// <summary>A cook trial with this <c>expected</c> and as many turns, each answering its number.</summary>
    private static Trial Cook(string expected, int turns) =>
        Trial(CookEvalMode, "cook-x", expected, "Malmesbury",
            Enumerable.Range(1, turns).Select(n => Turn(n, [Text($"antwoord {n}")])).ToArray());

    /// <summary>It is a model check that reads <c>serves</c> and is not required.</summary>
    [Test]
    public void ItIsAModelGrader_ThatReadsServes_AndIsNotRequired()
    {
        var grader = Grader();

        Assert.That((grader.Name, grader.Kind, grader.Required),
            Is.EqualTo(("meal-for-people", CheckKind.Model, false)));
        Assert.That(grader.ExpectedKeys, Is.EquivalentTo(new[] { "serves" }));
    }

    /// <summary>
    /// What the judge is sent changed (the conversation so far, no budget), and the rubric's hash doesn't cover the
    /// user message, so the version says it: 2. An old grade of version 1 was made on the last answer alone, with the
    /// budget in view.
    /// </summary>
    [Test]
    public void WhatTheJudgeIsSentChangedIn6a4f_SoItIsVersion2()
    {
        Assert.That(Grader().Version, Is.EqualTo(2));
    }

    /// <summary>
    /// It judges the answer for the people asked, and the grade says which model judged with which rubric.
    /// </summary>
    [Test]
    public async Task ItJudgesTheAnswerForThePeopleAskedAndSaysWhatJudged()
    {
        var client = new MealForPeopleTests.ScriptedJudge("""{"meal": true, "reason": "feeds four"}""");

        var grade = await Grader(client).GradeAsync(Cook("""{"serves":4,"budget":150,"town":"Malmesbury"}""", turns: 1),
            CancellationToken.None);

        Assert.That((grade.Outcome, grade.Reason), Is.EqualTo((GradeOutcome.Pass, "feeds four")));
        Assert.That(grade.Judge, Is.EqualTo(new GradeJudge("qwen3:8b", CookFile.RubricHash)));
        var user = client.Sent.Single()[1].Text;
        // The judge is not told the budget. It must never judge it, and a small model judged it anyway when told;
        // another check checks the cost.
        Assert.That(user,
            Does.Contain("People: 4").And.Not.Contain("Budget").And.Contain("Town: Malmesbury").And
                .Contain("antwoord 1"));
    }

    /// <summary>The last answer's people are the last number asked up to its turn.</summary>
    [TestCase("""{"serves":[4,6]}""", 2, 6)]
    [TestCase("""{"serves":2}""", 2, 2)]
    [TestCase("""{"serves":[4,6]}""", 1, 4)]
    public async Task TheLastAnswersPeople_AreTheLastNumberAskedUpToItsTurn(string expected, int turns, int people)
    {
        var client = new MealForPeopleTests.ScriptedJudge("""{"meal": true, "reason": "r"}""");

        await Grader(client).GradeAsync(Cook(expected, turns), CancellationToken.None);

        var user = client.Sent.Single()[1].Text;
        // The earlier answers are sent too, under "the conversation so far", and the judged one last, under "the
        // answer to judge".
        Assert.That(user, Does.Contain($"People: {people}").And.EndWith($"The answer to judge:\nantwoord {turns}"));
        if (turns > 1)
            Assert.That(user, Does.Contain("The conversation so far:").And.Contain($"Assistant: antwoord {turns - 1}"));
    }

    /// <summary>A scenario that asks for no people has no meal to judge, and no request is made.</summary>
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

    /// <summary>With no judge configured, the grade is Unknown.</summary>
    [Test]
    public async Task WithNoJudgeConfigured_TheGradeIsUnknown()
    {
        var grade = await MealForPeopleCheck.Create(CookFile.Rubric, () => null)
            .GradeAsync(Cook("""{"serves":4}""", turns: 1), CancellationToken.None);

        Assert.That((grade.Outcome, grade.Reason), Is.EqualTo((GradeOutcome.Unknown, "no judge configured")));
    }

    /// <summary>A judge that can't tell gives Unknown, never a pass.</summary>
    [Test]
    public async Task AJudgeThatCantTell_IsUnknown_NeverAPass()
    {
        var client = new MealForPeopleTests.ScriptedJudge("""{"meal": null, "reason": "no quantities"}""");

        var grade = await Grader(client).GradeAsync(Cook("""{"serves":4}""", turns: 1), CancellationToken.None);

        Assert.That((grade.Outcome, grade.Reason), Is.EqualTo((GradeOutcome.Unknown, "no quantities")));
    }

    /// <summary>
    /// The cook's eval lists it after the fixed rules, then the person's review of its question, and the outcome
    /// doesn't wait on either.
    /// </summary>
    [Test]
    public void TheCookEvalHasIt_AfterTheFixedRules_AndTheOutcomeDoesNotWaitOnIt()
    {
        var checks = CookFile.Eval.Checks;

        var grader = checks.Single(g => g.Name == "meal-for-people");

        Assert.That((grader.Kind, grader.Required), Is.EqualTo((CheckKind.Model, false)));
        Assert.That(checks.Where(g => g.Required).Select(g => g.Kind), Has.All.EqualTo(CheckKind.FixedRule));
        Assert.That(checks[^2], Is.SameAs(grader), "the judge, then the person's review of its question");
        Assert.That(checks[^1].Name, Is.EqualTo("meal-for-people.human"));
    }
}
