using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common.Tools;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;
using static LouisAgent.Evals.Tests.Trials.TrialBuilder;

namespace LouisAgent.Evals.Tests.Grading.Common.Tools;

/// <summary>
/// The routed-to check: the first turn went to the role the eval expects, or the agent answered itself where it
/// should, and a failed or infrastructure-broken turn says so.
/// </summary>
public class RoutedToCheckTests
{
    /// <summary>Grades a trial with the check.</summary>
    private static async Task<Grade> GradeAsync(ICheck grader, Trial trial) =>
        await grader.GradeAsync(trial, CancellationToken.None);

    [Test]
    public async Task RoutedTo_PassesWhenTheFirstTurnWentToTheRole()
    {
        var grade = await GradeAsync(new RoutedToCheck(_ => SampleRole.Alpha),
            Trial(Turn(1, [Call(SampleRole.Alpha)]), Turn(2)));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Pass));
        Assert.That(grade.Reason, Does.Contain(SampleRole.Alpha));
    }

    [Test]
    public async Task RoutedTo_FailsWhenItWentToAnotherRole()
    {
        var grade = await GradeAsync(new RoutedToCheck(_ => SampleRole.Eta), Trial(Turn(1, [Call(SampleRole.Alpha)])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain(SampleRole.Alpha));
    }

    [Test]
    public async Task RoutedTo_FailsWhenItAnsweredItself()
    {
        var grade = await GradeAsync(new RoutedToCheck(_ => SampleRole.Alpha), Trial(Turn(1, [Text("Hallo")])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("answered itself"));
    }

    [Test]
    public async Task RoutedTo_IsUnknownWithoutAnyTurn()
    {
        var grade = await GradeAsync(new RoutedToCheck(_ => SampleRole.Alpha), Trial());

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Unknown));
    }

    [Test]
    public async Task RoutedTo_ExpectsTheRoleOfEachTrial()
    {
        // The eval says where each trial should go (Puls's routing expects a different role per question).
        var grader = new RoutedToCheck(trial => trial.Scenario.Id == "bobotie" ? SampleRole.Alpha : SampleRole.Eta);

        Assert.That((await GradeAsync(grader, Trial(Turn(1, [Call(SampleRole.Alpha)])))).Outcome,
            Is.EqualTo(GradeOutcome.Pass));
    }

    [Test]
    public async Task RoutedTo_PassesWhenAnsweringItselfIsExpected()
    {
        // The cook eval's Puls-only mode expects the agent to cook itself: answering itself is the right route there.
        var grade = await GradeAsync(new RoutedToCheck(_ => null), Trial(Turn(1, [Text("Hallo")])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Pass));
    }

    [Test]
    public async Task RoutedTo_FailsAHandOverWhenItsAnsweringItselfIsExpected()
    {
        var grade = await GradeAsync(new RoutedToCheck(_ => null), Trial(Turn(1, [Call(SampleRole.Alpha)])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain(SampleRole.Alpha));
    }

    [Test]
    public async Task RoutedTo_FailsAnErroredTurnEvenWhenTheRouteIsRight()
    {
        // As the Python check: not an error, and the expected role.
        var grade = await GradeAsync(new RoutedToCheck(_ => SampleRole.Alpha),
            Trial(Turn(1, [Call(SampleRole.Alpha), Error()])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("failed"));
    }

    [Test]
    public async Task RoutedTo_IsUnknownForAnInfrastructureFailure()
    {
        var grade = await GradeAsync(new RoutedToCheck(_ => SampleRole.Alpha),
            Trial(Turn(1, [Call(SampleRole.Alpha)], failure: TurnFailure.Timeout)));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Unknown));
    }

    [Test]
    public async Task RoutedTo_IgnoresASpecialistsCall()
    {
        // A specialist's own hand-over is not the turn's route: the agent answered itself.
        var grade = await GradeAsync(new RoutedToCheck(_ => null), Trial(Turn(1, [SubCall(SampleRole.Alpha)])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Pass));
    }

    [Test]
    public async Task RoutedTo_OnlyTheFirstTurnCounts()
    {
        var grade = await GradeAsync(new RoutedToCheck(_ => SampleRole.Alpha),
            Trial(Turn(1, [Text("Hallo")]), Turn(2, [Call(SampleRole.Alpha)])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
    }

    [Test]
    public void RoutedTo_DeclaresTheExpectedKeysItsEvalGivesIt()
    {
        // F18 step 5.4: an eval whose route is a scenario's `routes_to` says so, so the loader can check the key is read and fed.
        Assert.That(new RoutedToCheck(_ => null).ExpectedKeys, Is.Empty);
        Assert.That(new RoutedToCheck(_ => null, expectedKeys: new HashSet<string> { "routes_to" }).ExpectedKeys,
            Is.EqualTo(new[] { "routes_to" }));
    }
}
