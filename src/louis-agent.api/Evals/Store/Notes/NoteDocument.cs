using LouisAgent.Evals.Protocol.Parts.Conversations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Evals.Store.Notes;

/// <summary>
/// A person's note as MongoDB keeps it in the <c>run_notes</c> collection, one document per note, apart from the
/// run's document (the recorder replaces that whole document as the run grows, and would wipe them). A note on the
/// whole run leaves its conversation empty. A field added later never breaks a read.
/// </summary>
/// <remarks>
/// Metadata Mapping on a Data Transfer Object (Fowler, PoEAA): the record says how each field is stored and the
/// driver does the copying, as <see cref="Runs.RunDocument"/> explains. Its id is a new ObjectId per note, the one the
/// driver used to add on insert: it rises with each note, so sorting on it gives the order written (Louis's pick).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
[BsonIgnoreExtraElements]
internal sealed record NoteDocument(
    [property: BsonId] ObjectId Id,
    [property: BsonElement("run")] string Run,
    [property: BsonElement("text")] string Text,
    [property: BsonElement("at")] DateTimeOffset At,
    [property: BsonElement("mode")] string? Mode,
    [property: BsonElement("repeat")] int? Repeat,
    [property: BsonElement("task")] string? Task)
{
    /// <summary>The document for a note on a run, with a new id later than every one before it.</summary>
    public static NoteDocument Of(string run, RunNote note) =>
        new(ObjectId.GenerateNewId(), run, note.Text, note.At, note.Conversation?.Mode, note.Conversation?.Repeat,
            note.Conversation?.Task);

    /// <summary>The note this document holds; one with no conversation was on the whole run.</summary>
    public RunNote ToNote() =>
        new(Text, At, this is { Mode: { } mode, Repeat: { } repeat, Task: { } task }
            ? new ConversationKey(mode, repeat, task)
            : null);
}
