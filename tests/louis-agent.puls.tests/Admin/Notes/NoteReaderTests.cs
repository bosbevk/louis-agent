using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Puls.Admin.Notes;
using LouisAgent.Puls.Admin.Reviews;

namespace LouisAgent.Puls.Tests.Admin.Notes;

/// <summary>
/// Reading the notes out of the contract's run reply: in the order sent, with their conversations, and none for none.
/// </summary>
public class NoteReaderTests
{
    /// <summary>When the notes were written.</summary>
    private static readonly DateTimeOffset At = new(2026, 10, 27, 9, 0, 0, TimeSpan.Zero);

    /// <summary>Notes come back in order, and a conversation's note carries its key with the run's id.</summary>
    [Test]
    public void TheNotes_AreReadInOrder_AConversationsWithItsKey()
    {
        var notes = NoteReader.Of("run1",
        [
            new NoteRow("on the run", At, null),
            new NoteRow("on bobotie", At.AddMinutes(5), new ConversationKeyRow(SampleMode.Beta, 2, "bobotie"))
        ]);

        Assert.That(notes,
            Is.EqualTo(new[]
            {
                new RunNoteEntry("on the run", At, null), new RunNoteEntry("on bobotie", At.AddMinutes(5),
                    new ConversationRef("run1", SampleMode.Beta, 2, "bobotie"))
            }));
    }

    /// <summary>No notes, or an empty list, reads as no notes.</summary>
    [Test]
    public void ARunWithNoNotes_ReadsAsNone()
    {
        Assert.That(NoteReader.Of("run1", null), Is.Empty);
        Assert.That(NoteReader.Of("run1", []), Is.Empty);
    }
}
