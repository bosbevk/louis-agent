using System.Text.Json;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Puls.Admin.Notes;
using LouisAgent.Puls.Admin.Reviews;
using LouisAgent.Testing;

namespace LouisAgent.Puls.Tests.Admin.Notes;

/// <summary>
/// Reading the notes out of the API's answer: in the order sent, with their conversations, and none from an older API.
/// </summary>
public class NoteReaderTests
{
    /// <summary>The run JSON with the given notes, or with no notes list at all when null.</summary>
    private static JsonElement Run(string? notes) =>
        JsonDocument
            .Parse("{\"run\":{\"id\":\"run1\",\"check\":\"c\"}" + (notes is null ? "" : ",\"notes\":" + notes) + "}")
            .RootElement.Clone();

    /// <summary>Notes come back in the order sent, with their times, and a conversation note carries its key.</summary>
    [Test]
    public void TheNotes_AreReadInTheOrderSent_WithTheirTimes_AndTheirConversations()
    {
        // One note on the run and one on a conversation.
        var run = Run(SampleMode.Fill(
            "[{\"text\":\"on the run\",\"at\":\"2026-10-27T09:00:00+00:00\",\"conversation\":null},"
            + "{\"text\":\"on bobotie\",\"at\":\"2026-10-27T09:05:00+00:00\",\"conversation\":{\"mode\":\"<ModeBeta>\",\"repeat\":2,\"task\":\"bobotie\"}}]"));

        var notes = NoteReader.Of(run);

        Assert.That(notes, Is.EqualTo(new[]
        {
            new RunNoteEntry("on the run", new DateTimeOffset(2026, 10, 27, 9, 0, 0, TimeSpan.Zero), null),
            new RunNoteEntry("on bobotie", new DateTimeOffset(2026, 10, 27, 9, 5, 0, TimeSpan.Zero),
                new ConversationRef("run1", SampleMode.Beta, 2, "bobotie")),
        }));
    }

    /// <summary>The contract's notes read the same: in order, a conversation's with its key, none for none.</summary>
    [Test]
    public void TheContractsNotes_AreReadTheSameWay()
    {
        var at = new DateTimeOffset(2026, 10, 27, 9, 0, 0, TimeSpan.Zero);

        var notes = NoteReader.Of("run1",
        [
            new NoteRow("on the run", at, null),
            new NoteRow("on bobotie", at, new ConversationKeyRow(SampleMode.Beta, 2, "bobotie"))
        ]);

        Assert.That(notes,
            Is.EqualTo(new[]
            {
                new RunNoteEntry("on the run", at, null),
                new RunNoteEntry("on bobotie", at, new ConversationRef("run1", SampleMode.Beta, 2, "bobotie"))
            }));
        Assert.That(NoteReader.Of("run1", null), Is.Empty);
    }

    /// <summary>No notes list, or an empty one, reads as no notes.</summary>
    [TestCase(null)]
    [TestCase("[]")]
    public void ARunWithNoNotes_ReadsAsNone(string? notes) => Assert.That(NoteReader.Of(Run(notes)), Is.Empty);
}
