using System.Text.Json;
using LouisAgent.Api.Evals.Drafts.Samples;
using LouisAgent.Evals.Protocol.Parts.Conversations;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Attributes;

namespace LouisAgent.Api.Evals.Drafts.Store.Documents;

/// <summary>
/// A sample draft as MongoDB keeps it in <c>eval_drafts</c>: its id as the document's own, <c>kind</c> set to
/// <c>sample</c>, where it came from, the sample's fields flat beside them, and who made it and when. A field added
/// later never breaks a read.
/// </summary>
/// <remarks>
/// Metadata Mapping on a Data Transfer Object (Fowler, PoEAA), as the review store's <c>HumanGradeDocument</c>. The
/// expected values are a <see cref="BsonDocument"/>, the one field whose shape is decided at run time (ADR 0048 allows
/// that), turned from and back into JSON at this edge the way the turn store's <c>EventDocument</c> does its data.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
[BsonIgnoreExtraElements]
internal sealed record SampleDraftDocument(
    [property: BsonId] string Id,
    [property: BsonElement("kind")] DraftKind Kind,
    [property: BsonElement("eval")] string Eval,
    [property: BsonElement("identity")] string Identity,
    [property: BsonElement("run")] string Run,
    [property: BsonElement("mode")] string Mode,
    [property: BsonElement("repeat")] int Repeat,
    [property: BsonElement("task")] string Task,
    [property: BsonElement("sample_id")] string SampleId,
    [property: BsonElement("input")] IReadOnlyList<SampleMessageDocument> Input,
    [property: BsonElement("target")] string? Target,
    [property: BsonElement("place")] string? Place,
    [property: BsonElement("time_zone")] string TimeZone,
    [property: BsonElement("follow_ups")] IReadOnlyList<string> FollowUps,
    [property: BsonElement("expected")] BsonDocument Expected,
    [property: BsonElement("categories")] IReadOnlyList<string> Categories,
    [property: BsonElement("author")] string Author,
    [property: BsonElement("at")] DateTimeOffset At,
    [property: BsonElement("role")] string? Role = null)
{
    /// <summary>How the expected values are written back out as JSON: numbers as plain numbers.</summary>
    private static readonly JsonWriterSettings RelaxedJson =
        new() { OutputMode = JsonOutputMode.RelaxedExtendedJson };

    /// <summary>The document for a sample draft.</summary>
    public static SampleDraftDocument Of(SampleDraft d)
    {
        var (sample, metadata) = (d.Sample, d.Sample.Metadata);
        return new SampleDraftDocument(d.Id, DraftKind.Sample, d.Eval, d.Identity, d.Run, d.Conversation.Mode,
            d.Conversation.Repeat, d.Conversation.Task, sample.Id,
            [.. sample.Input.Select(SampleMessageDocument.Of)],
            sample.Target, metadata.Place, metadata.TimeZone, metadata.FollowUps,
            BsonDocument.Parse(JsonSerializer.Serialize(metadata.Expected)), metadata.Categories, d.Author, d.At,
            metadata.Role);
    }

    /// <summary>The sample draft this document holds.</summary>
    public SampleDraft ToDraft()
    {
        // The expected values back as JSON, cloned so they outlive the document they were parsed into.
        using var expected = JsonDocument.Parse(Expected.ToJson(RelaxedJson));
        var metadata = new SampleBodyMetadata(Place, TimeZone, FollowUps, expected.RootElement.Clone(), Categories,
            Role);
        return new SampleDraft(Id, Eval, Identity, Run, new ConversationKey(Mode, Repeat, Task),
            new SampleBody(SampleId, [.. Input.Select(m => m.ToMessage())], metadata, Target), Author, At);
    }
}

/// <summary>One chat message of a sample draft's input, as its document holds it.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
[BsonIgnoreExtraElements]
internal sealed record SampleMessageDocument(
    [property: BsonElement("role")] string Role,
    [property: BsonElement("content")] string Content)
{
    /// <summary>The document for a message.</summary>
    public static SampleMessageDocument Of(SampleBodyMessage m) => new(m.Role, m.Content);

    /// <summary>The message this document holds.</summary>
    public SampleBodyMessage ToMessage() => new(Role, Content);
}
