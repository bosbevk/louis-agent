using LouisAgent.Evals.Trials;
using static LouisAgent.Evals.Tests.Trials.TrialBuilder;

namespace LouisAgent.Evals.Tests.Trials;

// The one reader of the engine's events (F18 step 4.2). Written first: the reader threw NotImplementedException on the red run.
public class EngineEventsTests
{
    [Test]
    public void TheCostIsTheUsageTheStreamReports()
    {
        Assert.That(EngineEvents.CostOf(Turn(1, [Usage("0.25")])), Is.EqualTo(0.25m));
    }

    [Test]
    public void AFailedTurnsCostIsReadFromItsError()
    {
        Assert.That(EngineEvents.CostOf(Turn(1, [ErrorWithUsage("0.1")])), Is.EqualTo(0.1m));
    }

    [Test]
    public void AnUnpricedRequestMakesTheCostUnknown()
    {
        // The total is the priced requests only: one more request had no price, so the turn's cost isn't known.
        Assert.That(EngineEvents.CostOf(Turn(1, [Usage("0.25", unpriced: 1)])), Is.Null);
    }

    [Test]
    public void ANullCostIsUnknown()
    {
        Assert.That(EngineEvents.CostOf(Turn(1, [Usage("null")])), Is.Null);
    }

    [Test]
    public void ATurnWithoutUsageHasAnUnknownCost()
    {
        Assert.That(EngineEvents.CostOf(Turn(1, [Text("Hallo")])), Is.Null);
    }

    [Test]
    public void AStreamedErrorIsTheTurnsError()
    {
        Assert.That(EngineEvents.StreamedError(Turn(1, [Error()])), Is.True);
        Assert.That(EngineEvents.StreamedError(Turn(1, [Text("Hallo")])), Is.False);
    }

    [Test]
    public void AnInfrastructureFailureIsReportedApart()
    {
        // A timeout is the infrastructure's, not the agent's: it is reported as its own failure, and no error event.
        Assert.That(EngineEvents.InfrastructureFailure(Turn(1, failure: TrialFailure.Timeout)), Is.EqualTo(TrialFailure.Timeout));
        Assert.That(EngineEvents.StreamedError(Turn(1, failure: TrialFailure.Timeout)), Is.False);
        Assert.That(EngineEvents.InfrastructureFailure(Turn(1, [Text("Hallo")])), Is.Null);
    }

    [Test]
    public void ASpecialistsErrorIsNotTheTurnsError()
    {
        Assert.That(EngineEvents.StreamedError(Turn(1, [SubError()])), Is.False);
    }

    [Test]
    public void ASpecialistsUsageIsNotAddedToTheTurnsCost()
    {
        Assert.That(EngineEvents.CostOf(Turn(1, [SubUsage("0.5"), Usage("0.25")])), Is.EqualTo(0.25m));
    }

    [Test]
    public void ASpecialistsCallIsNotTheTurnsRoute()
    {
        Assert.That(EngineEvents.RoutedRole(Turn(1, [SubCall("puls-cook")])), Is.Null);
    }

    [Test]
    public void TheRoleIsTheFirstStartSubSessionsRole()
    {
        Assert.That(EngineEvents.RoutedRole(Turn(1, [Call("puls-cook")])), Is.EqualTo("puls-cook"));
        Assert.That(EngineEvents.RoutedRole(Turn(1, [Text("Hallo")])), Is.Null);
    }
}
