using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common.Facts;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;
using static LouisAgent.Evals.Tests.Trials.TrialBuilder;

namespace LouisAgent.Evals.Tests.Grading.Common.Facts;

// The run-fact checks (F18 step 4.2): no error, answered within a time, cost under a limit. Written first: they threw
// NotImplementedException on the red run. The routed-to check's tests are in Tools/RoutedToCheckTests.
public class FactsChecksTests
{
    private static async Task<Grade> GradeAsync(ICheck grader, Trial trial) =>
        await grader.GradeAsync(trial, CancellationToken.None);

    [Test]
    public async Task NoError_PassesACleanTrial()
    {
        var grade = await GradeAsync(new NoErrorCheck(), Trial(Turn(1, [Text("Hallo")]), Turn(2)));

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Pass, 1.0)));
    }

    [Test]
    public async Task NoError_FailsAndNamesTheTurnThatRaisedAnError()
    {
        var grade = await GradeAsync(new NoErrorCheck(), Trial(Turn(1), Turn(2, [Error()])));

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Fail, 0.0)));
        Assert.That(grade.Reason, Does.Contain("turn 2"));
    }

    [Test]
    public async Task NoError_TheReasonCarriesTheErrorsMessage()
    {
        var grade = await GradeAsync(new NoErrorCheck(), Trial(Turn(1), Turn(2, [Error()])));

        Assert.That(grade.Reason, Does.Contain("overloaded"));
    }

    [Test]
    public async Task NoError_IsUnknownForAnInfrastructureFailure()
    {
        // A timeout is reported apart and is Unknown: it is not the agent's error.
        var grade = await GradeAsync(new NoErrorCheck(), Trial(Turn(1, failure: TurnFailure.Timeout)));

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Unknown, 0.0)));
        Assert.That(grade.Reason, Does.Contain("Timeout"));
    }

    [Test]
    public async Task NoError_IgnoresASpecialistsError()
    {
        var grade = await GradeAsync(new NoErrorCheck(), Trial(Turn(1, [SubError()])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Pass));
    }

    [Test]
    public void NoError_IsRequiredUnlessSaidOtherwise()
    {
        Assert.That(new NoErrorCheck().Required, Is.True);
        Assert.That(new NoErrorCheck(required: false).Required, Is.False);
    }

    [Test]
    public async Task WithinTime_PassesWhenEveryTurnIsWithinTheLimit()
    {
        var grade = await GradeAsync(new WithinTimeCheck(TimeSpan.FromSeconds(10)),
            Trial(Turn(1, seconds: 1), Turn(2, seconds: 10)));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Pass));
    }

    [Test]
    public async Task WithinTime_FailsAndNamesTheSlowTurn()
    {
        var grade = await GradeAsync(new WithinTimeCheck(TimeSpan.FromSeconds(10)),
            Trial(Turn(1, seconds: 1), Turn(2, seconds: 11)));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("turn 2"));
    }

    [Test]
    public async Task WithinTime_IsUnknownForAnInfrastructureFailure()
    {
        // A timed-out turn's duration is the timeout, not the agent's time: the grader can't tell.
        var grade = await GradeAsync(new WithinTimeCheck(TimeSpan.FromSeconds(10)),
            Trial(Turn(1, seconds: 60, failure: TurnFailure.Timeout)));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Unknown));
    }

    [Test]
    public async Task CostUnder_IsUnknownForAnInfrastructureFailure()
    {
        var grade = await GradeAsync(new CostUnderCheck(1m),
            Trial(Turn(1, [Usage("0.25")], failure: TurnFailure.Timeout)));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Unknown));
    }

    [Test]
    public async Task CostUnder_PassesWhenTheSummedCostIsUnderTheLimit()
    {
        var grade = await GradeAsync(new CostUnderCheck(1m), Trial(Turn(1, [Usage("0.25")]), Turn(2, [Usage("0.5")])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Pass));
        Assert.That(grade.Reason, Does.Contain("0.75"));
    }

    [Test]
    public async Task CostUnder_PassesAtTheLimit()
    {
        var grade = await GradeAsync(new CostUnderCheck(0.75m),
            Trial(Turn(1, [Usage("0.25")]), Turn(2, [Usage("0.5")])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Pass));
    }

    [Test]
    public async Task CostUnder_FailsWhenOverTheLimit()
    {
        var grade = await GradeAsync(new CostUnderCheck(0.5m),
            Trial(Turn(1, [Usage("0.25")]), Turn(2, [Usage("0.5")])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("0.75"));
    }

    [Test]
    public async Task CostUnder_IsUnknownWhenACostIsUnknown()
    {
        // Unknown is never a pass, and never a fail: the grader can't tell.
        var grade = await GradeAsync(new CostUnderCheck(1m),
            Trial(Turn(1, [Usage("0.25")]), Turn(2, [Usage("null")])));

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Unknown, 0.0)));
    }

    [Test]
    public async Task CostUnder_IsUnknownWithoutUsage()
    {
        // A turn that reported no usage is not a turn with a price: the grader can't tell, and says which.
        var grade = await GradeAsync(new CostUnderCheck(1m),
            Trial(Turn(1, [Usage("0.25")]), Turn(2, [Text("Hallo")])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Unknown));
        Assert.That(grade.Reason, Does.Contain("reported no usage"));
    }

    [Test]
    public async Task CostUnder_IsUnknownInAnotherCurrency()
    {
        var grade = await GradeAsync(new CostUnderCheck(1m), Trial(Turn(1, [Usage("0.25", currency: "ZAR")])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Unknown));
        Assert.That(grade.Reason, Does.Contain("ZAR"));
    }

    [Test]
    public async Task CostUnder_PassesWhenThePriceIsInUsd()
    {
        var grade = await GradeAsync(new CostUnderCheck(1m), Trial(Turn(1, [Usage("0.25", currency: "USD")])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Pass));
    }

    [Test]
    public async Task CostUnder_PassesWhenThePriceIsInLowerCaseUsd()
    {
        // The currency's case is not the grader's to judge: "usd" is USD, as the API's own comparison reads it.
        var grade = await GradeAsync(new CostUnderCheck(1m), Trial(Turn(1, [Usage("0.25", currency: "usd")])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Pass));
    }

    [Test]
    public async Task CostUnder_IsUnknownWhenARequestWasUnpriced()
    {
        var grade = await GradeAsync(new CostUnderCheck(1m), Trial(Turn(1, [Usage("0.25", unpriced: 1)])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Unknown));
    }
}
