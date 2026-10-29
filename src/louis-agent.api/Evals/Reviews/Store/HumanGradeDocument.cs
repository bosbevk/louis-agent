using LouisAgent.Evals.Protocol.Parts.Conversations;
using MongoDB.Bson.Serialization.Attributes;

namespace LouisAgent.Api.Evals.Reviews.Store;

/// <summary>
/// A person's grade as MongoDB keeps it in the <c>human_grades</c> collection: which conversation, which human
/// review, the verdict by its wire name, the note, who gave it and when. Grades are added, never updated, so it has no
/// id of its own: Mongo gives it one on insert. A field added later never breaks a read.
/// </summary>
/// <remarks>
/// Metadata Mapping on a Data Transfer Object (Fowler, PoEAA): the record says how each field is stored and the
/// driver does the copying, as the eval store's <c>RunDocument</c> does. The properties keep the order the hand
/// mapping wrote, so a document is the same field for field.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
[BsonIgnoreExtraElements]
internal sealed record HumanGradeDocument(
    [property: BsonElement("run")] string Run,
    [property: BsonElement("mode")] string Mode,
    [property: BsonElement("repeat")] int Repeat,
    [property: BsonElement("task")] string Task,
    [property: BsonElement("grader")] string Grader,
    [property: BsonElement("verdict")] HumanVerdict Verdict,
    [property: BsonElement("note")] string Note,
    [property: BsonElement("reviewer")] string Reviewer,
    [property: BsonElement("at")] DateTimeOffset At)
{
    /// <summary>The document for a person's grade.</summary>
    public static HumanGradeDocument Of(HumanGrade g) =>
        new(g.Run, g.Conversation.Mode, g.Conversation.Repeat, g.Conversation.Task, g.Grader, g.Verdict, g.Note,
            g.Reviewer, g.At);

    /// <summary>The grade this document holds.</summary>
    public HumanGrade ToGrade() =>
        new(Run, new ConversationKey(Mode, Repeat, Task), Grader, Verdict, Note, Reviewer, At);
}
