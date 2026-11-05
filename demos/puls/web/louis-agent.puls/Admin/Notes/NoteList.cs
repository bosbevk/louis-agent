using LouisAgent.Puls.Admin.Reviews;

namespace LouisAgent.Puls.Admin.Notes;

/// <summary>Picks which notes a panel shows and puts them in order.</summary>
public static class NoteList
{
    /// <summary>A place's notes, newest first: the run's own, or one conversation's. If two share a time, the later one comes first.</summary>
    public static IReadOnlyList<RunNoteEntry> Newest(IReadOnlyList<RunNoteEntry> all, ConversationRef? conversation) =>
        all.Select((note, written) => (note, written))
            .Where(x => x.note.Conversation == conversation)
            .OrderByDescending(x => x.note.At).ThenByDescending(x => x.written)
            .Select(x => x.note)
            .ToArray();
}
