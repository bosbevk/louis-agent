using LouisAgent.Puls.Admin.Notes;
using LouisAgent.Puls.Admin.Reviews;
using LouisAgent.Testing;

namespace LouisAgent.Puls.Tests.Admin.Notes;

/// <summary>Which notes a panel shows, and that they come newest first.</summary>
public class NoteListTests
{
    /// <summary>The moment the tests count from.</summary>
    private static readonly DateTimeOffset T0 = new(2026, 10, 27, 9, 0, 0, TimeSpan.Zero);

    /// <summary>A conversation key.</summary>
    private static readonly ConversationRef Bobotie = new("run1", SampleMode.Beta, 1, "bobotie");

    /// <summary>Another conversation key.</summary>
    private static readonly ConversationRef Sosaties = new("run1", SampleMode.Beta, 1, "sosaties");

    /// <summary>A note a number of minutes after T0.</summary>
    private static RunNoteEntry Note(string text, int minutes, ConversationRef? conversation = null) =>
        new(text, T0.AddMinutes(minutes), conversation);

    /// <summary>The run's panel shows only the notes that aren't on a conversation, newest first.</summary>
    [Test]
    public void TheRunsNotes_AreTheOnesOnNoConversation_NewestFirst()
    {
        var all = new[] { Note("old", 1), Note("on bobotie", 2, Bobotie), Note("new", 3) };

        Assert.That(NoteList.Newest(all, null).Select(n => n.Text), Is.EqualTo(new[] { "new", "old" }));
    }

    /// <summary>A conversation's panel shows only its own notes.</summary>
    [Test]
    public void AConversationsNotes_AreOnlyThatConversations()
    {
        var all = new[]
        {
            Note("run", 1), Note("bobotie 1", 2, Bobotie), Note("sosaties", 3, Sosaties), Note("bobotie 2", 4, Bobotie)
        };

        Assert.That(NoteList.Newest(all, Bobotie).Select(n => n.Text), Is.EqualTo(new[] { "bobotie 2", "bobotie 1" }));
    }

    /// <summary>Two notes with the same time: the later one is shown first.</summary>
    [Test]
    public void NotesWithTheSameTime_ShowTheLaterWrittenFirst()
    {
        var all = new[] { Note("first", 1), Note("second", 1) };

        Assert.That(NoteList.Newest(all, null).Select(n => n.Text), Is.EqualTo(new[] { "second", "first" }));
    }

    /// <summary>No notes gives an empty list.</summary>
    [Test]
    public void NoNotes_IsAnEmptyList() => Assert.That(NoteList.Newest([], Bobotie), Is.Empty);
}
