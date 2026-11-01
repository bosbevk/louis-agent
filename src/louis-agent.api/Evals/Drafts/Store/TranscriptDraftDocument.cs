using LouisAgent.Evals.Protocol.Parts.Conversations;
using MongoDB.Bson.Serialization.Attributes;

namespace LouisAgent.Api.Evals.Drafts.Store;

/// <summary>
/// A transcript draft as MongoDB keeps it in <c>eval_drafts</c>: its id as the document's own, <c>kind</c> set to
/// <c>transcript</c>, where it came from, a reference or a known-bad and why, its file name, its protocol lines one
/// string each, and who kept it and when. A field added later never breaks a read.
/// </summary>
/// <remarks>
/// Metadata Mapping on a Data Transfer Object (Fowler, PoEAA), as the review store's <c>HumanGradeDocument</c>; the
/// kinds are stored by their wire names.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
[BsonIgnoreExtraElements]
internal sealed record TranscriptDraftDocument(
    [property: BsonId] string Id,
    [property: BsonElement("kind")] DraftKind Kind,
    [property: BsonElement("eval")] string Eval,
    [property: BsonElement("identity")] string Identity,
    [property: BsonElement("run")] string Run,
    [property: BsonElement("mode")] string Mode,
    [property: BsonElement("repeat")] int Repeat,
    [property: BsonElement("task")] string Task,
    [property: BsonElement("transcript_kind")]
    TranscriptKind TranscriptKind,
    [property: BsonElement("reason")] string? Reason,
    [property: BsonElement("file_name")] string FileName,
    [property: BsonElement("lines")] IReadOnlyList<string> Lines,
    [property: BsonElement("author")] string Author,
    [property: BsonElement("at")] DateTimeOffset At)
{
    /// <summary>The document for a transcript draft.</summary>
    public static TranscriptDraftDocument Of(TranscriptDraft d) =>
        new(d.Id, DraftKind.Transcript, d.Eval, d.Identity, d.Run, d.Conversation.Mode, d.Conversation.Repeat,
            d.Conversation.Task, d.Kind, d.Reason, d.FileName, d.Lines, d.Author, d.At);

    /// <summary>The transcript draft this document holds.</summary>
    public TranscriptDraft ToDraft() =>
        new(Id, Eval, Identity, Run, new ConversationKey(Mode, Repeat, Task), TranscriptKind, Reason, FileName, Lines,
            Author, At);
}
