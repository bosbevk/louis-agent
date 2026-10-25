using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Puls.Grading.FixedRules;
using LouisAgent.Evals.Trials;
using static LouisAgent.Evals.Puls.Tests.PulsTrials;

namespace LouisAgent.Evals.Puls.Tests.Grading.FixedRules;

// F18 step 5.4: Puls's route is the scenario's `routes_to`, judged by the harness's RoutedToGrader when the mode can hand a question on, and
// "answers itself" when it can't. These are the cases the cook's and the routing's own route graders held (CookRoutedRight, RoutingRoutedRight),
// moved here when RoutedToGrader took over; the grader's own rules are in RoutedToGraderTests.
public class PulsRouteTests
{
    private const string CookRoute = """{"routes_to":"puls-cook"}""";
    private const string SelfRoute = """{"routes_to":null}""";

    private static Task<Grade> Grade(EvalMode mode, string expected, params TurnTranscript[] turns) =>
        PulsRoute.Grader().GradeAsync(Trial(mode, "x", expected, null, turns), CancellationToken.None).AsTask();

    [Test]
    public void TheGraderIsTheCommonRoutedTo_Required_ReadingTheRoutesToKey()
    {
        var grader = PulsRoute.Grader();

        Assert.That((grader.Name, grader.Required, grader.Kind), Is.EqualTo(("routed-to", true, GraderKind.FixedRule)));
        Assert.That(grader.ExpectedKeys, Is.EqualTo(new[] { "routes_to" }));
    }

    [Test]
    public async Task AMealGoesToTheCook_InAModeThatCanHandItOn() =>
        Assert.That((await Grade(CookMode, CookRoute, Turn(1, [Start("puls-cook"), Text("Resep.")]))).Outcome, Is.EqualTo(GradeOutcome.Pass));

    [Test]
    public async Task AModeIsOnTheCookBecauseOfItsToolsets_NotItsName() =>
        Assert.That((await Grade(new EvalMode("puls-cook-sonnet", "puls-cook", ["specialists", "web"]), CookRoute,
            Turn(1, [Start("puls-cook"), Text("Resep.")]))).Outcome, Is.EqualTo(GradeOutcome.Pass));

    [Test]
    public async Task PulsAnsweringAMealItselfIsWrong_WhereTheCookIsOn()
    {
        var grade = await Grade(CookMode, CookRoute, Turn(1, [Text("Resep.")]));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("answered itself"));
    }

    [Test]
    public async Task AModeThatCannotHandItOn_ExpectsPulsToAnswerItself() =>
        Assert.That((await Grade(ToolsMode, CookRoute, Turn(1, [Text("Die koers is 10.5%.")]))).Outcome, Is.EqualTo(GradeOutcome.Pass));

    [Test]
    public async Task TheBaselineIsRightToAnswerAMealItself() =>
        Assert.That((await Grade(new EvalMode("baseline", Role, ["web", "files"]), CookRoute, Turn(1, [Text("Resep.")]))).Outcome, Is.EqualTo(GradeOutcome.Pass));

    [Test]
    public async Task AWebQuestionHandedToTheCookIsWrong()
    {
        var grade = await Grade(CookMode, SelfRoute, Turn(1, [Start("puls-cook")]));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("puls-cook"));
    }

    [Test]
    public async Task AnErroredTurnIsNeverRight_AndTheReasonSaysItFailed()
    {
        var grade = await Grade(CookMode, SelfRoute, Turn(1, [Error()]));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("failed"));
    }

    [Test]
    public async Task TheRouteIsTheFirstTurns_ALaterTurnsErrorIsNoReasonForItsMiss()
    {
        var grade = await Grade(CookMode, CookRoute, Turn(1, [Text("Resep.")]), Turn(2, [Error()]));

        Assert.That(grade.Reason, Does.Contain("answered itself").And.Not.Contain("error"));
    }
}
