using LouisAgent.Evals.Trials;
using static LouisAgent.Evals.Tests.Grading.Common.TrialBuilder;

namespace LouisAgent.Evals.Tests.Grading.Common;

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
    public void AnErrorEventOrAFailureIsAnError()
    {
        Assert.That(EngineEvents.RaisedError(Turn(1, [Error()])), Is.True);
        Assert.That(EngineEvents.RaisedError(Turn(1, failure: TrialFailure.Timeout)), Is.True);
        Assert.That(EngineEvents.RaisedError(Turn(1, [Text("Hallo")])), Is.False);
    }

    [Test]
    public void TheRoleIsTheFirstStartSubSessionsRole()
    {
        Assert.That(EngineEvents.RoutedRole(Turn(1, [Call("puls-cook")])), Is.EqualTo("puls-cook"));
        Assert.That(EngineEvents.RoutedRole(Turn(1, [Text("Hallo")])), Is.Null);
    }
}
