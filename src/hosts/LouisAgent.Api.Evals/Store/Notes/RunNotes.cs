using static LouisAgent.Api.Evals.Store.EvalDocuments;
using LouisAgent.Evals.Protocol.Parts.Conversations;

namespace LouisAgent.Api.Evals.Store.Notes;

/// <summary>
/// The one rule for what counts as a note. Both stores use it, so memory and Mongo turn down the same texts.
/// </summary>
public static class RunNotes
{
    /// <summary>The longest note allowed, counted after trimming.</summary>
    public const int MaxLength = 2000;

    /// <summary>Turns typed text into a note, or says why it can't be one.</summary>
    /// <remarks>Guard clauses: each way the text is turned down returns first, so the note is the last line.</remarks>
    public static NoteResult Make(string? text, DateTimeOffset at, ConversationKey? conversation)
    {
        // Spaces around a note count for nothing, neither for being blank nor for being too long.
        string trimmed = (text ?? "").Trim();

        if (trimmed.Length == 0) return new NoteResult.Refused(NoteRefusal.Blank, 0);
        if (trimmed.Length > MaxLength) return new NoteResult.Refused(NoteRefusal.TooLong, trimmed.Length);

        return new NoteResult.Added(new RunNote(trimmed, at, conversation));
    }
}
