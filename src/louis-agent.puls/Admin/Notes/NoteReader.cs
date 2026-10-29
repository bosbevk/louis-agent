using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Puls.Admin.Reviews;

namespace LouisAgent.Puls.Admin.Notes;

/// <summary>
/// Turns the run reply's notes into the entries the notes panel shows, a conversation's note with a link to it.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public static class NoteReader
{
    /// <summary>The notes of a run read as the contract's record, in the order written.</summary>
    public static IReadOnlyList<RunNoteEntry> Of(string runId, IReadOnlyList<NoteRow>? notes) =>
    [
        .. (notes ?? []).Select(n => new RunNoteEntry(n.Text, n.At,
            n.Conversation is { } key ? new ConversationRef(runId, key.Mode, key.Repeat, key.Task) : null))
    ];
}
