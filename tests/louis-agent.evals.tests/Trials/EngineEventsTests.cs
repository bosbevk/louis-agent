using LouisAgent.Testing;
using LouisAgent.Evals.Trials;
using LouisAgent.SessionStream;
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
        Assert.That(EngineEvents.RoutedRole(Turn(1, [SubCall(SampleRole.Alpha)])), Is.Null);
    }

    [Test]
    public void AContentBlockWithNoNameIsNoRoute()
    {
        // An odd event is read as saying nothing, rather than throwing and failing the whole grade.
        var odd = SessionEvent.From("content_block_start", Json("""{"type":"content_block_start","index":0,"content_block":{"id":"t1"}}"""));
        var notAnObject = SessionEvent.From("content_block_start", Json("""{"type":"content_block_start","index":0,"content_block":"text"}"""));

        Assert.That(EngineEvents.RoutedRole(Turn(1, [odd])), Is.Null);
        Assert.That(EngineEvents.RoutedRole(Turn(1, [notAnObject])), Is.Null);
    }

    [Test]
    public void AnUnpricedCountThatIsNotANumberMakesTheCostUnknown()
    {
        var odd = SessionEvent.From("message_delta", Json("""{"type":"message_delta","delta":{"stop_reason":"end_turn"},"usage":{"cost":0.25,"unpriced":null}}"""));

        Assert.That(EngineEvents.CostOf(Turn(1, [odd])), Is.Null);
    }

    [Test]
    public void TheErrorsMessageIsReadFromTheStream()
    {
        Assert.That(EngineEvents.ErrorMessageOf(Turn(1, [Error()])), Is.EqualTo("overloaded"));
        Assert.That(EngineEvents.ErrorMessageOf(Turn(1, [Text("Hallo")])), Is.Null);
    }

    [Test]
    public void ABlankRoleIsNoRoute()
    {
        var empty = SessionEvent.From("content_block_start", Json("""{"type":"content_block_start","index":0,"content_block":{"type":"tool_use","id":"t1","name":"StartSubSession","input":{"role":""}}}"""));
        var spaces = SessionEvent.From("content_block_start", Json("""{"type":"content_block_start","index":0,"content_block":{"type":"tool_use","id":"t1","name":"StartSubSession","input":{"role":"   "}}}"""));

        Assert.That(EngineEvents.RoutedRole(Turn(1, [empty])), Is.Null);
        Assert.That(EngineEvents.RoutedRole(Turn(1, [spaces])), Is.Null);
    }

    [Test]
    public void HasUsageIsWhetherTheTurnReportedUsage()
    {
        Assert.That(EngineEvents.HasUsage(Turn(1, [Usage("0.25")])), Is.True);
        Assert.That(EngineEvents.HasUsage(Turn(1, [Text("Hallo")])), Is.False);
    }

    [Test]
    public void TheCurrencyIsTheUsagesCurrency()
    {
        Assert.That(EngineEvents.CurrencyOf(Turn(1, [Usage("0.25", currency: "ZAR")])), Is.EqualTo("ZAR"));
        Assert.That(EngineEvents.CurrencyOf(Turn(1, [Usage("0.25")])), Is.Null);
    }

    [Test]
    public void TheRoleIsTheFirstStartSubSessionsRole()
    {
        Assert.That(EngineEvents.RoutedRole(Turn(1, [Call(SampleRole.Alpha)])), Is.EqualTo(SampleRole.Alpha));
        Assert.That(EngineEvents.RoutedRole(Turn(1, [Text("Hallo")])), Is.Null);
    }

    [Test]
    public void TheErrorText_IsItsMessage_ElseItsJson_ElseNothing()
    {
        // F18 step 5.3: the reader Puls's TurnView builds on. A message-less error is still an error, so its JSON is the text.
        var noMessage = SessionEvent.From("error", Json("""{"type":"error","error":"the connection dropped"}"""));

        Assert.That(EngineEvents.ErrorTextOf(Turn(1, [Error()])), Is.EqualTo("overloaded"));
        Assert.That(EngineEvents.ErrorTextOf(Turn(1, [noMessage])), Does.Contain("the connection dropped"));
        Assert.That(EngineEvents.ErrorTextOf(Turn(1, [Text("Hallo")])), Is.Null);
    }
}
