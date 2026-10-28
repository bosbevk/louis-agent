using LouisAgent.Testing;
using System.Text.Json;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Protocol.Events.Conversations;
using LouisAgent.Evals.Protocol.Events.Run;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.SessionStream;

namespace LouisAgent.Evals.Tests.Protocol;

/// <summary>
/// A conversation read back from the protocol's own lines: each turn's session, question, time and failure, and its
/// stream events in order, whichever version wrote the lines.
/// </summary>
public class ProtocolTranscriptsTests
{
    /// <summary>A version 1 run's lines: one conversation, and another mode's error line beside it.</summary>
    private static readonly string[] Lines =
    [
        "@{\"event\": \"protocol\", \"t\": 1.0, \"version\": 1}",
        SampleMode.Fill("@{\"event\": \"mode\", \"t\": 1.0, \"mode\": \"<ModeBeta>\", \"repeat\": 1, \"toolsets\": [], "
                        + "\"role\": \"puls/voice-af\", \"llm\": null, \"conversations\": 1}"),
        SampleMode.Fill("@{\"event\": \"question\", \"t\": 1.0, \"mode\": \"<ModeBeta>\", \"repeat\": 1, "
                        + "\"task\": \"bobotie\", \"turn\": 1, \"session\": \"sess_1\", \"text\": \"Hallo\"}"),
        SampleMode.Fill("@{\"event\": \"sse\", \"t\": 1.0, \"mode\": \"<ModeBeta>\", \"repeat\": 1, "
                        + "\"task\": \"bobotie\", \"turn\": 1, \"type\": \"content_block_start\", "
                        + "\"data\": {\"type\": \"content_block_start\", \"index\": 0, "
                        + "\"content_block\": {\"type\": \"text\", \"text\": \"\"}}}"),
        SampleMode.Fill("@{\"event\": \"sse\", \"t\": 1.0, \"mode\": \"<ModeBeta>\", \"repeat\": 1, "
                        + "\"task\": \"bobotie\", \"turn\": 1, \"type\": \"content_block_delta\", "
                        + "\"data\": {\"type\": \"content_block_delta\", \"index\": 0, "
                        + "\"delta\": {\"type\": \"text_delta\", \"text\": \"Hallo\"}}}"),
        SampleMode.Fill("@{\"event\": \"turn\", \"t\": 1.0, \"mode\": \"<ModeBeta>\", \"repeat\": 1, "
                        + "\"task\": \"bobotie\", \"turn\": 1, \"session\": \"sess_1\", \"question\": \"Hallo\", "
                        + "\"answer\": \"Hallo\", \"role\": null, \"continued\": null, \"puls_calls\": 0, "
                        + "\"specialist_calls\": 0, \"hosts\": [], \"seconds\": 0.5, \"error\": null}"),
        SampleMode.Fill("@{\"event\": \"sse\", \"t\": 1.0, \"mode\": \"<ModeAlpha>\", \"repeat\": 1, "
                        + "\"task\": \"bobotie\", \"turn\": 1, \"type\": \"error\", \"data\": {\"type\": \"error\"}}"),
        SampleMode.Fill("@{\"event\": \"conversation\", \"t\": 1.0, \"mode\": \"<ModeBeta>\", \"repeat\": 1, "
                        + "\"task\": \"bobotie\", \"role\": null, \"scores\": {\"routed_right\": true}, \"errors\": 0, "
                        + "\"seconds\": 0.5}"),
    ];

    /// <summary>One conversation's turn: its number, session, question and time.</summary>
    [Test]
    public void ReadsTheTurnsOfOneConversation()
    {
        var transcript = ProtocolTranscripts.Read(Lines, SampleMode.Beta, 1, "bobotie");

        var turn = transcript.Turns.Single();
        Assert.That((turn.Turn, turn.Session, turn.Question), Is.EqualTo((1, "sess_1", "Hallo")));
        Assert.That(turn.Duration, Is.EqualTo(TimeSpan.FromSeconds(0.5)));
    }

    /// <summary>A turn's stream events, in the order they came.</summary>
    [Test]
    public void KeepsEachTurnsEventsInOrder()
    {
        var events = ProtocolTranscripts.Read(Lines, SampleMode.Beta, 1, "bobotie").Turns.Single().Events;

        Assert.That(events.Select(e => e.Type), Is.EqualTo(new[] { "content_block_start", "content_block_delta" }));
        Assert.That(events[1], Is.InstanceOf<ContentBlockDelta>());
    }

    /// <summary>The conversation's mode, from its mode line.</summary>
    [Test]
    public void ReadsTheModeOfTheConversation()
    {
        var mode = ProtocolTranscripts.ReadMode(Lines);

        Assert.That((mode.Name, mode.Role), Is.EqualTo((SampleMode.Beta, "puls/voice-af")));
    }

    /// <summary>A file of one conversation is read whole, with no filter.</summary>
    [Test]
    public void ReadsEveryLineOfAFileWithNoFilter()
    {
        // A file is one conversation: the reader takes all its lines, the other mode's error line too.
        var transcript = ProtocolTranscripts.ReadConversation(Lines);

        Assert.That(transcript.Turns.Select(turn => turn.Turn), Is.EqualTo(new[] { 1 }));
        Assert.That(transcript.Turns.Single().Events.Select(e => e.Type), Does.Contain("error"));
    }

    /// <summary>Another mode's line never reaches this conversation's turn.</summary>
    [Test]
    public void LeavesOutAnotherModesLines()
    {
        // The puls-only error line is another conversation's: it doesn't reach this one's turn.
        var events = ProtocolTranscripts.Read(Lines, SampleMode.Beta, 1, "bobotie").Turns.Single().Events;

        Assert.That(events.OfType<ErrorReported>(), Is.Empty);
    }

    /// <summary>The same conversation as version 2 lines, written by the contract.</summary>
    private static string[] V2Lines(TurnFailure? failure = null)
    {
        var at = new DateTimeOffset(2026, 10, 28, 8, 0, 0, TimeSpan.Zero);
        EvalEvent[] events =
        [
            new ModeEvent(at, SampleMode.Beta, 1, [], "puls/voice-af", null, 1),
            new QuestionEvent(at, SampleMode.Beta, 1, "bobotie", 1, "sess_1", "Hallo"),
            new SseEvent(at, SampleMode.Beta, 1, "bobotie", 1, "content_block_start",
                Json("""{"type":"content_block_start","index":0,"content_block":{"type":"text","text":""}}""")),
            new SseEvent(at, SampleMode.Beta, 1, "bobotie", 1, "content_block_delta",
                Json("""{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Hallo"}}""")),
            new TurnEvent(at, SampleMode.Beta, 1, "bobotie", 1, "sess_1", "Hallo", "Hallo", null, null, 0, 0, [], 0.5,
                null, failure),
        ];
        return [.. events.Select(EvalProtocolJson.Line)];
    }

    /// <summary>A bit of JSON text as an element that outlives its document.</summary>
    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    /// <summary>
    /// Version 2 lines read into the same transcript as version 1's: the turn, its events and its time.
    /// </summary>
    [Test]
    public void ReadsAConversationWrittenAsVersion2()
    {
        var turn = ProtocolTranscripts.Read(V2Lines(), SampleMode.Beta, 1, "bobotie").Turns.Single();

        Assert.That((turn.Turn, turn.Session, turn.Question), Is.EqualTo((1, "sess_1", "Hallo")));
        Assert.That(turn.Duration, Is.EqualTo(TimeSpan.FromSeconds(0.5)));
        Assert.That(turn.Events.Select(e => e.Type),
            Is.EqualTo(new[] { "content_block_start", "content_block_delta" }));
    }

    /// <summary>The mode of a version 2 conversation reads as version 1's does.</summary>
    [Test]
    public void ReadsTheModeOfAVersion2Conversation()
    {
        var mode = ProtocolTranscripts.ReadMode(V2Lines());

        Assert.That((mode.Name, mode.Role), Is.EqualTo((SampleMode.Beta, "puls/voice-af")));
    }

    /// <summary>A version 2 turn's failure reads in the contract's words.</summary>
    [Test]
    public void ReadsAVersion2TurnsFailure()
    {
        var turn = ProtocolTranscripts.ReadConversation(V2Lines(TurnFailure.ApiError)).Turns.Single();

        Assert.That(turn.Failure, Is.EqualTo(TurnFailure.ApiError));
    }

    /// <summary>A version 1 turn's failure still reads in version 1's spelling, with its underscore.</summary>
    [Test]
    public void ReadsAVersion1TurnsFailure_InItsOldSpelling()
    {
        // The v1 turn line above, with the failure v1 wrote for an API refusal.
        string v1Word = V1Words.FailureAliases.Single(alias => alias.Value == TurnFailure.ApiError).Key;
        string[] lines =
        [
            .. Lines.Select(line => line.Contains("\"event\": \"turn\"")
                ? line.Replace("\"error\": null}", $"\"error\": null, \"failure\": \"{v1Word}\"}}")
                : line)
        ];

        var turn = ProtocolTranscripts.Read(lines, SampleMode.Beta, 1, "bobotie").Turns.Single();

        Assert.That(turn.Failure, Is.EqualTo(TurnFailure.ApiError));
    }
}
