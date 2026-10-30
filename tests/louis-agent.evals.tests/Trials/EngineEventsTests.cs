using LouisAgent.Testing;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;
using LouisAgent.SessionStream;
using static LouisAgent.Evals.Tests.Trials.TrialBuilder;

namespace LouisAgent.Evals.Tests.Trials;

/// <summary>
/// The one reader of the engine's events: a turn's cost, tokens, currency, error, route and tools, read from the
/// stream as the API sends it, and an odd event read as saying nothing rather than throwing.
/// </summary>
public class EngineEventsTests
{
    /// <summary>The cost is the usage the stream reports at the turn's end.</summary>
    [Test]
    public void TheCostIsTheUsageTheStreamReports()
    {
        Assert.That(EngineEvents.CostOf(Turn(1, [Usage("0.25")])), Is.EqualTo(0.25m));
    }

    /// <summary>A turn that failed still has a cost, read from the usage on its error.</summary>
    [Test]
    public void AFailedTurnsCostIsReadFromItsError()
    {
        Assert.That(EngineEvents.CostOf(Turn(1, [ErrorWithUsage("0.1")])), Is.EqualTo(0.1m));
    }

    /// <summary>One request with no price leaves the turn's cost unknown, never too low.</summary>
    [Test]
    public void AnUnpricedRequestMakesTheCostUnknown()
    {
        // The total is the priced requests only: one more request had no price, so the turn's cost isn't known.
        Assert.That(EngineEvents.CostOf(Turn(1, [Usage("0.25", unpriced: 1)])), Is.Null);
    }

    /// <summary>A null cost is unknown, not zero.</summary>
    [Test]
    public void ANullCostIsUnknown()
    {
        Assert.That(EngineEvents.CostOf(Turn(1, [Usage("null")])), Is.Null);
    }

    /// <summary>A turn that reported no usage has no known cost.</summary>
    [Test]
    public void ATurnWithoutUsageHasAnUnknownCost()
    {
        Assert.That(EngineEvents.CostOf(Turn(1, [Text("Hallo")])), Is.Null);
    }

    /// <summary>A turn's tokens are its four counts summed: input, both cache counts and output.</summary>
    [Test]
    public void ATurnsTokens_AreItsFourCountsSummed()
    {
        Assert.That(EngineEvents.TokensOf(Turn(1, [Tokens(8, 20572, 26125, 1582)])), Is.EqualTo(48287));
    }

    /// <summary>
    /// A tool's inputs are those of its calls, the agent's first and then its specialists', and no other tool's.
    /// </summary>
    [Test]
    public void AToolsInputs_AreTheAgentsThenTheSpecialists()
    {
        var turn = Turn(1, [
            SubToolCall("FindProducts", """{"shop":"pnp"}"""), ToolCall("FindProducts", """{"shop":"checkers"}"""),
            ToolCall("WebSearch", """{"query":"x"}""")
        ]);

        Assert.That(
            EngineEvents.ToolInputsOf(turn, "FindProducts").Select(input => input.GetProperty("shop").GetString()),
            Is.EqualTo(new[] { "checkers", "pnp" }));
    }

    /// <summary>A missing count, or no usage at all, leaves the tokens unknown, never a smaller sum.</summary>
    [Test]
    public void AMissingCount_OrNoUsage_LeavesTheTokensUnknown()
    {
        var missing = SessionEvent.From("message_delta", Json(
            """{"type":"message_delta","usage":{"input_tokens":8,"cache_creation_input_tokens":1,"output_tokens":3}}"""));

        Assert.That(EngineEvents.TokensOf(Turn(1, [missing])), Is.Null);
        Assert.That(EngineEvents.TokensOf(Turn(1, [Text("Hallo")])), Is.Null);
    }

    /// <summary>An error event in the stream is the turn's error; a plain answer has none.</summary>
    [Test]
    public void AStreamedErrorIsTheTurnsError()
    {
        Assert.That(EngineEvents.StreamedError(Turn(1, [Error()])), Is.True);
        Assert.That(EngineEvents.StreamedError(Turn(1, [Text("Hallo")])), Is.False);
    }

    /// <summary>A timeout is the infrastructure's failure, reported apart from the agent's errors.</summary>
    [Test]
    public void AnInfrastructureFailureIsReportedApart()
    {
        // A timeout is the infrastructure's, not the agent's: it is reported as its own failure, and no error event.
        Assert.That(EngineEvents.InfrastructureFailure(Turn(1, failure: TurnFailure.Timeout)),
            Is.EqualTo(TurnFailure.Timeout));
        Assert.That(EngineEvents.StreamedError(Turn(1, failure: TurnFailure.Timeout)), Is.False);
        Assert.That(EngineEvents.InfrastructureFailure(Turn(1, [Text("Hallo")])), Is.Null);
    }

    /// <summary>A specialist's error, inside its sub-session, isn't the turn's error.</summary>
    [Test]
    public void ASpecialistsErrorIsNotTheTurnsError()
    {
        Assert.That(EngineEvents.StreamedError(Turn(1, [SubError()])), Is.False);
    }

    /// <summary>A specialist's usage isn't added again: the turn's own usage already holds the whole cost.</summary>
    [Test]
    public void ASpecialistsUsageIsNotAddedToTheTurnsCost()
    {
        Assert.That(EngineEvents.CostOf(Turn(1, [SubUsage("0.5"), Usage("0.25")])), Is.EqualTo(0.25m));
    }

    /// <summary>A specialist handing work on isn't where the turn was routed.</summary>
    [Test]
    public void ASpecialistsCallIsNotTheTurnsRoute()
    {
        Assert.That(EngineEvents.RoutedRole(Turn(1, [SubCall(SampleRole.Alpha)])), Is.Null);
    }

    /// <summary>A content block with no name, or one that isn't an object, is no route.</summary>
    [Test]
    public void AContentBlockWithNoNameIsNoRoute()
    {
        // An odd event is read as saying nothing, rather than throwing and failing the whole grade.
        var odd = SessionEvent.From("content_block_start",
            Json("""{"type":"content_block_start","index":0,"content_block":{"id":"t1"}}"""));
        var notAnObject = SessionEvent.From("content_block_start",
            Json("""{"type":"content_block_start","index":0,"content_block":"text"}"""));

        Assert.That(EngineEvents.RoutedRole(Turn(1, [odd])), Is.Null);
        Assert.That(EngineEvents.RoutedRole(Turn(1, [notAnObject])), Is.Null);
    }

    /// <summary>An unpriced count that isn't a number leaves the cost unknown.</summary>
    [Test]
    public void AnUnpricedCountThatIsNotANumberMakesTheCostUnknown()
    {
        var odd = SessionEvent.From("message_delta",
            Json(
                """{"type":"message_delta","delta":{"stop_reason":"end_turn"},"usage":{"cost":0.25,"unpriced":null}}"""));

        Assert.That(EngineEvents.CostOf(Turn(1, [odd])), Is.Null);
    }

    /// <summary>The error's message is read from the stream; a turn without an error has none.</summary>
    [Test]
    public void TheErrorsMessageIsReadFromTheStream()
    {
        Assert.That(EngineEvents.ErrorMessageOf(Turn(1, [Error()])), Is.EqualTo("overloaded"));
        Assert.That(EngineEvents.ErrorMessageOf(Turn(1, [Text("Hallo")])), Is.Null);
    }

    /// <summary>A hand-over to a blank role is no route.</summary>
    [Test]
    public void ABlankRoleIsNoRoute()
    {
        var empty = SessionEvent.From("content_block_start",
            Json(
                """{"type":"content_block_start","index":0,"content_block":{"type":"tool_use","id":"t1","name":"StartSubSession","input":{"role":""}}}"""));
        var spaces = SessionEvent.From("content_block_start",
            Json(
                """{"type":"content_block_start","index":0,"content_block":{"type":"tool_use","id":"t1","name":"StartSubSession","input":{"role":"   "}}}"""));

        Assert.That(EngineEvents.RoutedRole(Turn(1, [empty])), Is.Null);
        Assert.That(EngineEvents.RoutedRole(Turn(1, [spaces])), Is.Null);
    }

    /// <summary>A turn has usage only when its stream reported some.</summary>
    [Test]
    public void HasUsageIsWhetherTheTurnReportedUsage()
    {
        Assert.That(EngineEvents.HasUsage(Turn(1, [Usage("0.25")])), Is.True);
        Assert.That(EngineEvents.HasUsage(Turn(1, [Text("Hallo")])), Is.False);
    }

    /// <summary>The currency is the one the usage names, and none when it names none.</summary>
    [Test]
    public void TheCurrencyIsTheUsagesCurrency()
    {
        Assert.That(EngineEvents.CurrencyOf(Turn(1, [Usage("0.25", currency: "ZAR")])), Is.EqualTo("ZAR"));
        Assert.That(EngineEvents.CurrencyOf(Turn(1, [Usage("0.25")])), Is.Null);
    }

    /// <summary>The route is the role of the turn's first hand-over, and none when the agent answered itself.</summary>
    [Test]
    public void TheRoleIsTheFirstStartSubSessionsRole()
    {
        Assert.That(EngineEvents.RoutedRole(Turn(1, [Call(SampleRole.Alpha)])), Is.EqualTo(SampleRole.Alpha));
        Assert.That(EngineEvents.RoutedRole(Turn(1, [Text("Hallo")])), Is.Null);
    }

    /// <summary>An error's text is its message, else its JSON, else nothing.</summary>
    [Test]
    public void TheErrorText_IsItsMessage_ElseItsJson_ElseNothing()
    {
        // A message-less error is still an error, so its JSON is the text.
        var noMessage = SessionEvent.From("error", Json("""{"type":"error","error":"the connection dropped"}"""));

        Assert.That(EngineEvents.ErrorTextOf(Turn(1, [Error()])), Is.EqualTo("overloaded"));
        Assert.That(EngineEvents.ErrorTextOf(Turn(1, [noMessage])), Does.Contain("the connection dropped"));
        Assert.That(EngineEvents.ErrorTextOf(Turn(1, [Text("Hallo")])), Is.Null);
    }
}
