using LouisAgent.Testing;
using System.Text.Json;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Protocol.Events.Conversations;
using LouisAgent.Evals.Protocol.Events.Run;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Protocol.Parts.Conversations;
using LouisAgent.SessionStream;

namespace LouisAgent.Evals.Tests.Protocol;

/// <summary>
/// A conversation read back from the protocol's own lines: each turn's session, question, time and failure, and its
/// stream events in order. Only the contract's events are read; a version 1 <c>@</c> line is a person's line now.
/// </summary>
public class ProtocolTranscriptsTests
{
    /// <summary>The time every line carries.</summary>
    private static readonly DateTimeOffset At = new(2026, 10, 28, 8, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// A run's lines: one conversation, another mode's error line beside it, a person's line, and the conversation's
    /// result.
    /// </summary>
    private static string[] Lines(TurnFailure? failure = null)
    {
        EvalEvent[] events =
        [
            new ModeEvent(At, SampleMode.Beta, 1, [], "puls/voice-af", null, 1),
            new QuestionEvent(At, SampleMode.Beta, 1, "bobotie", 1, "sess_1", "Hallo"),
            new SseEvent(At, SampleMode.Beta, 1, "bobotie", 1, "content_block_start",
                Json("""{"type":"content_block_start","index":0,"content_block":{"type":"text","text":""}}""")),
            new SseEvent(At, SampleMode.Beta, 1, "bobotie", 1, "content_block_delta",
                Json("""{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Hallo"}}""")),
            new TurnEvent(At, SampleMode.Beta, 1, "bobotie", 1, "sess_1", "Hallo", "Hallo", null, null, 0, 0, [], 0.5,
                null, failure),
            new SseEvent(At, SampleMode.Alpha, 1, "bobotie", 1, "error", Json("""{"type":"error"}""")),
            new ConversationEvent(At, SampleMode.Beta, 1, "bobotie", null, 0, 0.5, [], TrialOutcome.Pass,
                new Metrics(0, 0, [], 1, null, null, 0.5, null)),
        ];
        return ["Running the bobotie eval.", .. events.Select(EvalProtocolJson.Line)];
    }

    /// <summary>A bit of JSON text as an element that outlives its document.</summary>
    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    /// <summary>One conversation's turn: its number, session, question and time.</summary>
    [Test]
    public void ReadsTheTurnsOfOneConversation()
    {
        var transcript = ProtocolTranscripts.Read(Lines(), SampleMode.Beta, 1, "bobotie");

        var turn = transcript.Turns.Single();
        Assert.That((turn.Turn, turn.Session, turn.Question), Is.EqualTo((1, "sess_1", "Hallo")));
        Assert.That(turn.Duration, Is.EqualTo(TimeSpan.FromSeconds(0.5)));
    }

    /// <summary>A turn's stream events, in the order they came.</summary>
    [Test]
    public void KeepsEachTurnsEventsInOrder()
    {
        var events = ProtocolTranscripts.Read(Lines(), SampleMode.Beta, 1, "bobotie").Turns.Single().Events;

        Assert.That(events.Select(e => e.Type), Is.EqualTo(new[] { "content_block_start", "content_block_delta" }));
        Assert.That(events[1], Is.InstanceOf<ContentBlockDelta>());
    }

    /// <summary>The conversation's mode, from its mode line.</summary>
    [Test]
    public void ReadsTheModeOfTheConversation()
    {
        var mode = ProtocolTranscripts.ReadMode(Lines());

        Assert.That((mode.Name, mode.Role), Is.EqualTo((SampleMode.Beta, "puls/voice-af")));
    }

    /// <summary>A file of one conversation is read whole, with no filter.</summary>
    [Test]
    public void ReadsEveryLineOfAFileWithNoFilter()
    {
        // A file is one conversation: the reader takes all its lines, the other mode's error line too.
        var transcript = ProtocolTranscripts.ReadConversation(Lines());

        Assert.That(transcript.Turns.Select(turn => turn.Turn), Is.EqualTo(new[] { 1 }));
        Assert.That(transcript.Turns.Single().Events.Select(e => e.Type), Does.Contain("error"));
    }

    /// <summary>Another mode's line never reaches this conversation's turn.</summary>
    [Test]
    public void LeavesOutAnotherModesLines()
    {
        var events = ProtocolTranscripts.Read(Lines(), SampleMode.Beta, 1, "bobotie").Turns.Single().Events;

        Assert.That(events.OfType<ErrorReported>(), Is.Empty);
    }

    /// <summary>A turn's failure reads in the contract's words.</summary>
    [Test]
    public void ReadsATurnsFailure()
    {
        var turn = ProtocolTranscripts.ReadConversation(Lines(TurnFailure.ApiError)).Turns.Single();

        Assert.That(turn.Failure, Is.EqualTo(TurnFailure.ApiError));
    }

    /// <summary>A version 1 <c>@</c> line is no event of the contract's, so it reads as nothing.</summary>
    [Test]
    public void AVersion1Line_ReadsAsNothing()
    {
        string[] v1 =
        [
            """@{"event": "question", "t": 1.0, "mode": "m", "repeat": 1, "task": "t", "turn": 1, "session": "s", """
            + """ "text": "Hallo"}""",
            """@{"event": "turn", "t": 1.0, "mode": "m", "repeat": 1, "task": "t", "turn": 1, "session": "s", """
            + """ "question": "Hallo", "answer": "Hallo", "seconds": 0.5}""",
        ];

        Assert.That(ProtocolTranscripts.ReadConversation(v1).Turns, Is.Empty);
    }
}
