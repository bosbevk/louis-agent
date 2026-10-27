using LouisAgent.Testing;
using LouisAgent.Evals.Protocol;
using LouisAgent.SessionStream;

namespace LouisAgent.Evals.Tests.Protocol;

// A conversation read back from the protocol's own lines (F18 step 4.2b). Written first: the reader threw NotImplementedException on the
// red run.
public class ProtocolTranscriptsTests
{
    private static readonly string[] Lines =
    [
        "@{\"event\": \"protocol\", \"t\": 1.0, \"version\": 1}",
        SampleMode.Fill("@{\"event\": \"mode\", \"t\": 1.0, \"mode\": \"<ModeBeta>\", \"repeat\": 1, \"toolsets\": [], \"role\": \"puls/voice-af\", \"llm\": null, \"conversations\": 1}"),
        SampleMode.Fill("@{\"event\": \"question\", \"t\": 1.0, \"mode\": \"<ModeBeta>\", \"repeat\": 1, \"task\": \"bobotie\", \"turn\": 1, \"session\": \"sess_1\", \"text\": \"Hallo\"}"),
        SampleMode.Fill("@{\"event\": \"sse\", \"t\": 1.0, \"mode\": \"<ModeBeta>\", \"repeat\": 1, \"task\": \"bobotie\", \"turn\": 1, \"type\": \"content_block_start\", \"data\": {\"type\": \"content_block_start\", \"index\": 0, \"content_block\": {\"type\": \"text\", \"text\": \"\"}}}"),
        SampleMode.Fill("@{\"event\": \"sse\", \"t\": 1.0, \"mode\": \"<ModeBeta>\", \"repeat\": 1, \"task\": \"bobotie\", \"turn\": 1, \"type\": \"content_block_delta\", \"data\": {\"type\": \"content_block_delta\", \"index\": 0, \"delta\": {\"type\": \"text_delta\", \"text\": \"Hallo\"}}}"),
        SampleMode.Fill("@{\"event\": \"turn\", \"t\": 1.0, \"mode\": \"<ModeBeta>\", \"repeat\": 1, \"task\": \"bobotie\", \"turn\": 1, \"session\": \"sess_1\", \"question\": \"Hallo\", \"answer\": \"Hallo\", \"role\": null, \"continued\": null, \"puls_calls\": 0, \"specialist_calls\": 0, \"hosts\": [], \"seconds\": 0.5, \"error\": null}"),
        SampleMode.Fill("@{\"event\": \"sse\", \"t\": 1.0, \"mode\": \"<ModeAlpha>\", \"repeat\": 1, \"task\": \"bobotie\", \"turn\": 1, \"type\": \"error\", \"data\": {\"type\": \"error\"}}"),
        SampleMode.Fill("@{\"event\": \"conversation\", \"t\": 1.0, \"mode\": \"<ModeBeta>\", \"repeat\": 1, \"task\": \"bobotie\", \"role\": null, \"scores\": {\"routed_right\": true}, \"errors\": 0, \"seconds\": 0.5}"),
    ];

    [Test]
    public void ReadsTheTurnsOfOneConversation()
    {
        var transcript = ProtocolTranscripts.Read(Lines, SampleMode.Beta, 1, "bobotie");

        var turn = transcript.Turns.Single();
        Assert.That((turn.Turn, turn.Session, turn.Question), Is.EqualTo((1, "sess_1", "Hallo")));
        Assert.That(turn.Duration, Is.EqualTo(TimeSpan.FromSeconds(0.5)));
    }

    [Test]
    public void KeepsEachTurnsEventsInOrder()
    {
        var events = ProtocolTranscripts.Read(Lines, SampleMode.Beta, 1, "bobotie").Turns.Single().Events;

        Assert.That(events.Select(e => e.Type), Is.EqualTo(new[] { "content_block_start", "content_block_delta" }));
        Assert.That(events[1], Is.InstanceOf<ContentBlockDelta>());
    }

    [Test]
    public void ReadsTheModeOfTheConversation()
    {
        var mode = ProtocolTranscripts.ReadMode(Lines);

        Assert.That((mode.Name, mode.Role), Is.EqualTo((SampleMode.Beta, "puls/voice-af")));
    }

    [Test]
    public void ReadsEveryLineOfAFileWithNoFilter()
    {
        // A file is one conversation: the reader takes all its lines, the other mode's error line too.
        var transcript = ProtocolTranscripts.ReadConversation(Lines);

        Assert.That(transcript.Turns.Select(turn => turn.Turn), Is.EqualTo(new[] { 1 }));
        Assert.That(transcript.Turns.Single().Events.Select(e => e.Type), Does.Contain("error"));
    }

    [Test]
    public void LeavesOutAnotherModesLines()
    {
        // The puls-only error line is another conversation's: it doesn't reach this one's turn.
        var events = ProtocolTranscripts.Read(Lines, SampleMode.Beta, 1, "bobotie").Turns.Single().Events;

        Assert.That(events.OfType<ErrorReported>(), Is.Empty);
    }
}
