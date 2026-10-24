using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Trials;
using static LouisAgent.Evals.Tests.Grading.Common.TrialBuilder;

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
    public async Task NoError_FailsAFailedTurn()
    {
        var grade = await GradeAsync(new NoErrorGrader(), Trial(Turn(1, failure: TrialFailure.Timeout)));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
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
    public async Task CostUnder_IsUnknownWhenARequestWasUnpriced()
    {
        var grade = await GradeAsync(new CostUnderGrader(1m), Trial(Turn(1, [Usage("0.25", unpriced: 1)])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Unknown));
    }

    [Test]
    public async Task RoutedTo_PassesWhenTheFirstTurnWentToTheRole()
    {
        var grade = await GradeAsync(new RoutedToGrader("puls-cook"), Trial(Turn(1, [Call("puls-cook")]), Turn(2)));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Pass));
        Assert.That(grade.Reason, Does.Contain("puls-cook"));
    }

    [Test]
    public async Task RoutedTo_FailsWhenItWentToAnotherRole()
    {
        var grade = await GradeAsync(new RoutedToGrader("puls-tools"), Trial(Turn(1, [Call("puls-cook")])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("puls-cook"));
    }

    [Test]
    public async Task RoutedTo_FailsWhenItAnsweredItself()
    {
        var grade = await GradeAsync(new RoutedToGrader("puls-cook"), Trial(Turn(1, [Text("Hallo")])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("answered itself"));
    }

    [Test]
    public async Task RoutedTo_IsUnknownWithoutAnyTurn()
    {
        var grade = await GradeAsync(new RoutedToGrader("puls-cook"), Trial());

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Unknown));
    }

    [Test]
    public async Task RoutedTo_OnlyTheFirstTurnCounts()
    {
        var grade = await GradeAsync(new RoutedToGrader("puls-cook"), Trial(Turn(1, [Text("Hallo")]), Turn(2, [Call("puls-cook")])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
    }
}
