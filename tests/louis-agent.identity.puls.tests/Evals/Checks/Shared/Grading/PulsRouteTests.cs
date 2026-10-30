using System.Text.Json;
using LouisAgent.Choices;
using LouisAgent.Evals;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;
using LouisAgent.Identities.Puls.Evals.Checks.Shared;
using static LouisAgent.Identities.Puls.Tests.Evals.Checks.PulsTrials;

namespace LouisAgent.Identities.Puls.Tests.Evals.Checks.Shared.Grading;

/// <summary>
/// Puls's route, as its eval files state it: the general <c>routed-to</c> entry reading the scenario's
/// <c>routes_to</c>, where only the specialists toolset hands a question on. A mode with it must send the question
/// to that role; a mode without it can only answer itself. These are the cases the cook's and the routing's own
/// route checks held.
/// </summary>
public class PulsRouteTests
{
    /// <summary>Puls's settings for the entry: a minion runs one tool, so it is no role to route to.</summary>
    private static readonly string Settings =
        $$"""{"expected_key": "routes_to", "delegating_toolsets": ["{{PulsToolset.Specialists.Wire()}}"]}""";

    private const string CookRoute = """{"routes_to":"puls-cook"}""";
    private const string SelfRoute = """{"routes_to":null}""";

    /// <summary>The route check built from Puls's settings, as an eval file builds it.</summary>
    private static ICheck Route() =>
        new RoutedToCheck.Entry().Create(JsonDocument.Parse(Settings).RootElement.Clone(), CheckContext.None);

    /// <summary>Grades a trial of a scenario with this <c>expected</c>, in this mode.</summary>
    private static Task<Grade> Grade(EvalMode mode, string expected, params TurnTranscript[] turns) =>
        Route().GradeAsync(Trial(mode, "x", expected, null, turns), CancellationToken.None).AsTask();

    /// <summary>It is the general routed-to, required, and names the key so the loader checks it is set.</summary>
    [Test]
    public void TheCheckIsTheGeneralRoutedTo_Required_ReadingTheRoutesToKey()
    {
        var check = Route();

        Assert.That((check.Name, check.Required, check.Kind), Is.EqualTo(("routed-to", true, CheckKind.FixedRule)));
        Assert.That(check.ExpectedKeys, Is.EqualTo(new[] { "routes_to" }));
    }

    /// <summary>A meal goes to the cook where the mode can hand it on.</summary>
    [Test]
    public async Task AMealGoesToTheCook_InAModeThatCanHandItOn() =>
        Assert.That(
            (await Grade(CookEvalMode, CookRoute, Turn(1, [Start(PulsEvalRole.Cook.Wire()), Text("Resep.")]))).Outcome,
            Is.EqualTo(GradeOutcome.Pass));

    /// <summary>What decides is the mode's toolsets, not its name.</summary>
    [Test]
    public async Task AModeIsOnTheCookBecauseOfItsToolsets_NotItsName() =>
        Assert.That((await Grade(
            new EvalMode("puls-cook-sonnet", PulsEvalRole.Cook.Wire(),
                [PulsToolset.Specialists.Wire(), PulsToolset.Web.Wire()]), CookRoute,
            Turn(1, [Start(PulsEvalRole.Cook.Wire()), Text("Resep.")]))).Outcome, Is.EqualTo(GradeOutcome.Pass));

    /// <summary>Where the cook is on, Puls answering a meal itself is wrong.</summary>
    [Test]
    public async Task PulsAnsweringAMealItselfIsWrong_WhereTheCookIsOn()
    {
        var grade = await Grade(CookEvalMode, CookRoute, Turn(1, [Text("Resep.")]));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("answered itself"));
    }

    /// <summary>A mode named for the cook, but without the specialists, can only answer itself.</summary>
    [Test]
    public async Task AModeThatCannotHandItOn_ExpectsPulsToAnswerItself() =>
        Assert.That((await Grade(ToolsEvalMode, CookRoute, Turn(1, [Text("Die koers is 10.5%.")]))).Outcome,
            Is.EqualTo(GradeOutcome.Pass));

    /// <summary>Minions alone are no hand-off: a minion runs one tool, it is no role to route to.</summary>
    [Test]
    public async Task MinionsAlone_CannotHandItOn() =>
        Assert.That((await Grade(
            new EvalMode("minions-only", PulsEvalRole.Cook.Wire(),
                [PulsToolset.Minions.Wire(), PulsToolset.Web.Wire()]), CookRoute,
            Turn(1, [Text("Resep.")]))).Outcome, Is.EqualTo(GradeOutcome.Pass));

    /// <summary>The routing eval's baseline has no specialists, so it is right to answer a meal itself.</summary>
    [Test]
    public async Task TheBaselineIsRightToAnswerAMealItself() =>
        Assert.That(
            (await Grade(new EvalMode("baseline", Role, [PulsToolset.Web.Wire(), PulsToolset.Files.Wire()]), CookRoute,
                Turn(1, [Text("Resep.")]))).Outcome, Is.EqualTo(GradeOutcome.Pass));

    /// <summary>A web question handed to the cook is wrong, and the reason names the cook.</summary>
    [Test]
    public async Task AWebQuestionHandedToTheCookIsWrong()
    {
        var grade = await Grade(CookEvalMode, SelfRoute, Turn(1, [Start(PulsEvalRole.Cook.Wire())]));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain(PulsEvalRole.Cook.Wire()));
    }

    /// <summary>A turn that errored is never right, and the reason says it failed.</summary>
    [Test]
    public async Task AnErroredTurnIsNeverRight_AndTheReasonSaysItFailed()
    {
        var grade = await Grade(CookEvalMode, SelfRoute, Turn(1, [Error()]));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("failed"));
    }

    /// <summary>The route is the first turn's: a later turn's error is no reason for its miss.</summary>
    [Test]
    public async Task TheRouteIsTheFirstTurns_ALaterTurnsErrorIsNoReasonForItsMiss()
    {
        var grade = await Grade(CookEvalMode, CookRoute, Turn(1, [Text("Resep.")]), Turn(2, [Error()]));

        Assert.That(grade.Reason, Does.Contain("answered itself").And.Not.Contain("error"));
    }
}
