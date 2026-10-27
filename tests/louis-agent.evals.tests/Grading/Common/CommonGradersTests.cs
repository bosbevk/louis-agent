using LouisAgent.Testing;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Trials;
using static LouisAgent.Evals.Tests.Trials.TrialBuilder;

namespace LouisAgent.Evals.Tests.Grading.Common;

// The four common graders (F18 step 4.2): no error, answered within a time, cost under a limit, routed to a role. Written first:
// they threw NotImplementedException on the red run.
public class CommonGradersTests
{
    private static async Task<Grade> GradeAsync(IGrader grader, Trial trial) => await grader.GradeAsync(trial, CancellationToken.None);

    [Test]
    public async Task NoError_PassesACleanTrial()
    {
        var grade = await GradeAsync(new NoErrorGrader(), Trial(Turn(1, [Text("Hallo")]), Turn(2)));

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Pass, 1.0)));
    }

    [Test]
    public async Task NoError_FailsAndNamesTheTurnThatRaisedAnError()
    {
        var grade = await GradeAsync(new NoErrorGrader(), Trial(Turn(1), Turn(2, [Error()])));

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Fail, 0.0)));
        Assert.That(grade.Reason, Does.Contain("turn 2"));
    }

    [Test]
    public async Task NoError_TheReasonCarriesTheErrorsMessage()
    {
        var grade = await GradeAsync(new NoErrorGrader(), Trial(Turn(1), Turn(2, [Error()])));

        Assert.That(grade.Reason, Does.Contain("overloaded"));
    }

    [Test]
    public async Task NoError_IsUnknownForAnInfrastructureFailure()
    {
        // A timeout is reported apart and is Unknown: it is not the agent's error.
        var grade = await GradeAsync(new NoErrorGrader(), Trial(Turn(1, failure: TrialFailure.Timeout)));

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Unknown, 0.0)));
        Assert.That(grade.Reason, Does.Contain("Timeout"));
    }

    [Test]
    public async Task NoError_IgnoresASpecialistsError()
    {
        var grade = await GradeAsync(new NoErrorGrader(), Trial(Turn(1, [SubError()])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Pass));
    }

    [Test]
    public void NoError_IsRequiredUnlessSaidOtherwise()
    {
        Assert.That(new NoErrorGrader().Required, Is.True);
        Assert.That(new NoErrorGrader(required: false).Required, Is.False);
    }

    [Test]
    public async Task WithinTime_PassesWhenEveryTurnIsWithinTheLimit()
    {
        var grade = await GradeAsync(new WithinTimeGrader(TimeSpan.FromSeconds(10)), Trial(Turn(1, seconds: 1), Turn(2, seconds: 10)));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Pass));
    }

    [Test]
    public async Task WithinTime_FailsAndNamesTheSlowTurn()
    {
        var grade = await GradeAsync(new WithinTimeGrader(TimeSpan.FromSeconds(10)), Trial(Turn(1, seconds: 1), Turn(2, seconds: 11)));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("turn 2"));
    }

    [Test]
    public async Task WithinTime_IsUnknownForAnInfrastructureFailure()
    {
        // A timed-out turn's duration is the timeout, not the agent's time: the grader can't tell.
        var grade = await GradeAsync(new WithinTimeGrader(TimeSpan.FromSeconds(10)), Trial(Turn(1, seconds: 60, failure: TrialFailure.Timeout)));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Unknown));
    }

    [Test]
    public async Task CostUnder_IsUnknownForAnInfrastructureFailure()
    {
        var grade = await GradeAsync(new CostUnderGrader(1m), Trial(Turn(1, [Usage("0.25")], failure: TrialFailure.Timeout)));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Unknown));
    }

    [Test]
    public async Task CostUnder_PassesWhenTheSummedCostIsUnderTheLimit()
    {
        var grade = await GradeAsync(new CostUnderGrader(1m), Trial(Turn(1, [Usage("0.25")]), Turn(2, [Usage("0.5")])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Pass));
        Assert.That(grade.Reason, Does.Contain("0.75"));
    }

    [Test]
    public async Task CostUnder_PassesAtTheLimit()
    {
        var grade = await GradeAsync(new CostUnderGrader(0.75m), Trial(Turn(1, [Usage("0.25")]), Turn(2, [Usage("0.5")])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Pass));
    }

    [Test]
    public async Task CostUnder_FailsWhenOverTheLimit()
    {
        var grade = await GradeAsync(new CostUnderGrader(0.5m), Trial(Turn(1, [Usage("0.25")]), Turn(2, [Usage("0.5")])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("0.75"));
    }

    [Test]
    public async Task CostUnder_IsUnknownWhenACostIsUnknown()
    {
        // Unknown is never a pass, and never a fail: the grader can't tell.
        var grade = await GradeAsync(new CostUnderGrader(1m), Trial(Turn(1, [Usage("0.25")]), Turn(2, [Usage("null")])));

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Unknown, 0.0)));
    }

    [Test]
    public async Task CostUnder_IsUnknownWithoutUsage()
    {
        // A turn that reported no usage is not a turn with a price: the grader can't tell, and says which.
        var grade = await GradeAsync(new CostUnderGrader(1m), Trial(Turn(1, [Usage("0.25")]), Turn(2, [Text("Hallo")])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Unknown));
        Assert.That(grade.Reason, Does.Contain("reported no usage"));
    }

    [Test]
    public async Task CostUnder_IsUnknownInAnotherCurrency()
    {
        var grade = await GradeAsync(new CostUnderGrader(1m), Trial(Turn(1, [Usage("0.25", currency: "ZAR")])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Unknown));
        Assert.That(grade.Reason, Does.Contain("ZAR"));
    }

    [Test]
    public async Task CostUnder_PassesWhenThePriceIsInUsd()
    {
        var grade = await GradeAsync(new CostUnderGrader(1m), Trial(Turn(1, [Usage("0.25", currency: "USD")])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Pass));
    }

    [Test]
    public async Task CostUnder_PassesWhenThePriceIsInLowerCaseUsd()
    {
        // The currency's case is not the grader's to judge: "usd" is USD, as the API's own comparison reads it.
        var grade = await GradeAsync(new CostUnderGrader(1m), Trial(Turn(1, [Usage("0.25", currency: "usd")])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Pass));
    }

    [Test]
    public async Task CostUnder_IsUnknownWhenARequestWasUnpriced()
    {
        var grade = await GradeAsync(new CostUnderGrader(1m), Trial(Turn(1, [Usage("0.25", unpriced: 1)])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Unknown));
    }

    [Test]
    public async Task RoutedTo_PassesWhenTheFirstTurnWentToTheRole()
    {
        var grade = await GradeAsync(new RoutedToGrader(_ => SampleRole.Alpha), Trial(Turn(1, [Call(SampleRole.Alpha)]), Turn(2)));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Pass));
        Assert.That(grade.Reason, Does.Contain(SampleRole.Alpha));
    }

    [Test]
    public async Task RoutedTo_FailsWhenItWentToAnotherRole()
    {
        var grade = await GradeAsync(new RoutedToGrader(_ => SampleRole.Eta), Trial(Turn(1, [Call(SampleRole.Alpha)])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain(SampleRole.Alpha));
    }

    [Test]
    public async Task RoutedTo_FailsWhenItAnsweredItself()
    {
        var grade = await GradeAsync(new RoutedToGrader(_ => SampleRole.Alpha), Trial(Turn(1, [Text("Hallo")])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("answered itself"));
    }

    [Test]
    public async Task RoutedTo_IsUnknownWithoutAnyTurn()
    {
        var grade = await GradeAsync(new RoutedToGrader(_ => SampleRole.Alpha), Trial());

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Unknown));
    }

    [Test]
    public async Task RoutedTo_ExpectsTheRoleOfEachTrial()
    {
        // The eval says where each trial should go (Puls's routing expects a different role per question).
        var grader = new RoutedToGrader(trial => trial.Scenario.Id == "bobotie" ? SampleRole.Alpha : SampleRole.Eta);

        Assert.That((await GradeAsync(grader, Trial(Turn(1, [Call(SampleRole.Alpha)])))).Outcome, Is.EqualTo(GradeOutcome.Pass));
    }

    [Test]
    public async Task RoutedTo_PassesWhenAnsweringItselfIsExpected()
    {
        // The cook eval's Puls-only mode expects the agent to cook itself: answering itself is the right route there.
        var grade = await GradeAsync(new RoutedToGrader(_ => null), Trial(Turn(1, [Text("Hallo")])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Pass));
    }

    [Test]
    public async Task RoutedTo_FailsAHandOverWhenItsAnsweringItselfIsExpected()
    {
        var grade = await GradeAsync(new RoutedToGrader(_ => null), Trial(Turn(1, [Call(SampleRole.Alpha)])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain(SampleRole.Alpha));
    }

    [Test]
    public async Task RoutedTo_FailsAnErroredTurnEvenWhenTheRouteIsRight()
    {
        // As the Python check: not an error, and the expected role.
        var grade = await GradeAsync(new RoutedToGrader(_ => SampleRole.Alpha), Trial(Turn(1, [Call(SampleRole.Alpha), Error()])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("failed"));
    }

    [Test]
    public async Task RoutedTo_IsUnknownForAnInfrastructureFailure()
    {
        var grade = await GradeAsync(new RoutedToGrader(_ => SampleRole.Alpha), Trial(Turn(1, [Call(SampleRole.Alpha)], failure: TrialFailure.Timeout)));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Unknown));
    }

    [Test]
    public async Task RoutedTo_IgnoresASpecialistsCall()
    {
        // A specialist's own hand-over is not the turn's route: the agent answered itself.
        var grade = await GradeAsync(new RoutedToGrader(_ => null), Trial(Turn(1, [SubCall(SampleRole.Alpha)])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Pass));
    }

    [Test]
    public async Task RoutedTo_OnlyTheFirstTurnCounts()
    {
        var grade = await GradeAsync(new RoutedToGrader(_ => SampleRole.Alpha), Trial(Turn(1, [Text("Hallo")]), Turn(2, [Call(SampleRole.Alpha)])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
    }

    [Test]
    public void RoutedTo_DeclaresTheExpectedKeysItsEvalGivesIt()
    {
        // F18 step 5.4: an eval whose route is a scenario's `routes_to` says so, so the loader can check the key is read and fed.
        Assert.That(new RoutedToGrader(_ => null).ExpectedKeys, Is.Empty);
        Assert.That(new RoutedToGrader(_ => null, expectedKeys: new HashSet<string> { "routes_to" }).ExpectedKeys, Is.EqualTo(new[] { "routes_to" }));
    }
}
