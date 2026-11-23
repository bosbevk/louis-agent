using LouisAgent.Evals.Contracts.Transcripts;
using LouisAgent.Evals.Transcripts;
using LouisAgent.SessionStream;
using static LouisAgent.Evals.Tests.Transcripts.StreamEvents;

namespace LouisAgent.Evals.Tests.Transcripts;

/// <summary>
/// A turn's stream, as the API writes it, becomes what happened in the turn: the answer as sent, each tool call with
/// its result as the agent got it, the turn's error, and its cost.
/// </summary>
public class TurnTranscriptsTests
{
    private const string Ask = "What is louis-agent?";
    private static readonly TimeSpan Took = TimeSpan.FromSeconds(7);

    /// <summary>The answer is the text deltas joined in the order they came; thinking is never part of it.</summary>
    [Test]
    public void TextDeltas_JoinIntoTheAnswer_InOrder()
    {
        SessionEvent[] events =
            [Started(), TextOpened(0), Thinking("hmm"), Text("An AI agent "), Text("framework in C#."), Closed(0)];

        var transcript = TurnTranscripts.Of(Ask, events, Took);

        Assert.That(transcript.Ask, Is.EqualTo(Ask));
        Assert.That(transcript.Answer, Is.EqualTo("An AI agent framework in C#."));
        Assert.That(transcript.Error, Is.Null);
    }

    /// <summary>A tool call is its name and its input, and its result is the result block whole.</summary>
    [Test]
    public void AToolCall_HasItsNameAndInput_AndItsResultWhole()
    {
        SessionEvent[] events =
        [
            Started(), ToolUse("t1", "ReadDoc", """{"path":"docs/README.md"}"""),
            ToolResult("t1", "# Docs\nThe whole page, line by line."), Text("It is a framework.")
        ];

        var transcript = TurnTranscripts.Of(Ask, events, Took);

        var expected = new ToolCallTranscript("ReadDoc", """{"path":"docs/README.md"}""",
            "# Docs\nThe whole page, line by line.");
        Assert.That(transcript.Calls, Is.EqualTo(new[] { expected }));
    }

    /// <summary>A result the stream marks as an error is a failed call.</summary>
    [Test]
    public void AResultThatFailed_IsMarkedFailed()
    {
        SessionEvent[] events =
            [Started(), ToolUse("t1", "ReadDoc", "{}"), ToolResult("t1", "Error: no such doc.", failed: true)];

        var transcript = TurnTranscripts.Of(Ask, events, Took);

        Assert.That(transcript.Calls.Single().Failed, Is.True);
    }

    /// <summary>An error event is the turn's error, its own message.</summary>
    [Test]
    public void AnErrorEvent_IsTheTurnsError()
    {
        SessionEvent[] events = [Started(), Text("Let me "), Failed("The model is overloaded.")];

        var transcript = TurnTranscripts.Of(Ask, events, Took);

        Assert.That(transcript.Error, Is.EqualTo("The model is overloaded."));
        Assert.That(transcript.Answer, Is.EqualTo("Let me "));
    }

    /// <summary>The cost and its currency are the turn's last usage numbers, on its end.</summary>
    [Test]
    public void TheCost_ComesFromTheTurnsLastUsage()
    {
        SessionEvent[] events = [Started(), Text("Hi."), Stopped(0.0042m, "USD")];

        var cost = TurnTranscripts.CostOf(events);

        Assert.That(cost, Is.EqualTo(new TurnCost(0.0042m, "USD")));
    }

    /// <summary>A failed turn still carries the usage it reached, and that is its cost.</summary>
    [Test]
    public void AFailedTurnsCost_IsTheUsageOnItsError()
    {
        SessionEvent[] events = [Started(), Failed("Overloaded.", 0.0007m, "USD")];

        var cost = TurnTranscripts.CostOf(events);

        Assert.That(cost, Is.EqualTo(new TurnCost(0.0007m, "USD")));
    }

    /// <summary>Usage nobody could price is a null cost, never 0, and so is a turn with no usage at all.</summary>
    [Test]
    public void ATurnWithNoPricedUsage_HasANullCost()
    {
        SessionEvent[] unpriced = [Started(), Text("Hi."), Stopped(null, null)];
        SessionEvent[] noUsage = [Started(), Text("Hi.")];

        Assert.That(TurnTranscripts.CostOf(unpriced), Is.EqualTo(new TurnCost(null, null)));
        Assert.That(TurnTranscripts.CostOf(noUsage), Is.EqualTo(new TurnCost(null, null)));
    }

    /// <summary>
    /// An asked agent's own work stays out of this turn's calls, while the call to <c>AskAgent</c> and its result,
    /// the asked agent's answer, are among them.
    /// </summary>
    [Test]
    public void AnAskedAgentsEvents_AreNotThisTurnsCalls_WhileTheAskIs()
    {
        const string askInput = """{"address":"triage.sorter","message":"Sort this."}""";
        SessionEvent[] events =
        [
            Started(), ToolUse("t1", "AskAgent", askInput),
            AskedAgents("t1", "content_block_start", ToolUseJson("c1", "ReadTable", "{}")),
            AskedAgents("t1", "content_block_delta",
                """{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Sorted."}}"""),
            ToolResult("t1", "Sorted."), Text("The sorter sorted it.")
        ];

        var transcript = TurnTranscripts.Of(Ask, events, Took);

        Assert.That(transcript.Calls, Is.EqualTo(new[] { new ToolCallTranscript("AskAgent", askInput, "Sorted.") }));
        Assert.That(transcript.Answer, Is.EqualTo("The sorter sorted it."));
    }

    /// <summary>How long the turn took is the time it was given, since the stream holds no clock.</summary>
    [Test]
    public void TheTimeItTook_IsTheOneItWasGiven()
    {
        var transcript = TurnTranscripts.Of(Ask, [Started(), Text("Hi.")], Took);

        Assert.That(transcript.Took, Is.EqualTo(Took));
    }
}
