using LouisAgent.Api.Evals.Drafts.Composed;
using MongoDB.Bson.Serialization.Attributes;

namespace LouisAgent.Api.Evals.Drafts.Store.Documents;

/// <summary>
/// An eval being composed, as MongoDB keeps it in <c>eval_drafts</c>: its id as the document's own, <c>kind</c> set
/// to <c>eval</c>, its name under <c>eval</c> as the other kinds file theirs, its identity, its file's text, its
/// rubrics, and who saved it last and when. A field added later never breaks a read.
/// </summary>
/// <remarks>
/// Metadata Mapping on a Data Transfer Object (Fowler, PoEAA), the third typed view of the one collection, beside
/// <see cref="SampleDraftDocument"/> and <see cref="TranscriptDraftDocument"/>.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
[BsonIgnoreExtraElements]
internal sealed record EvalDraftDocument(
    [property: BsonId] string Id,
    [property: BsonElement("kind")] DraftKind Kind,
    [property: BsonElement("eval")] string Eval,
    [property: BsonElement("identity")] string Identity,
    [property: BsonElement("json")] string Json,
    [property: BsonElement("rubrics")] IReadOnlyList<RubricDocument> Rubrics,
    [property: BsonElement("author")] string Author,
    [property: BsonElement("at")] DateTimeOffset At,
    [property: BsonElement("scenarios")] IReadOnlyList<RubricDocument>? Scenarios = null)
{
    /// <summary>The document for an eval draft.</summary>
    public static EvalDraftDocument Of(EvalDraft d) =>
        new(d.Id, DraftKind.Eval, d.Name, d.Identity, d.Json,
            [.. d.Rubrics.Select(r => new RubricDocument(r.Name, r.Text))], d.Author, d.At,
            [.. d.Scenarios.Select(s => new RubricDocument(s.Name, s.Text))]);

    /// <summary>The eval draft this document holds.</summary>
    public EvalDraft ToDraft() =>
        new(Id, Identity, Eval, Json, [.. Rubrics.Select(r => new RubricFile(r.Name, r.Text))], Author, At,
            [.. (Scenarios ?? []).Select(s => new RubricFile(s.Name, s.Text))]);
}

/// <summary>One rubric of an eval draft, as its document keeps it: the file's name and its text.</summary>
internal sealed record RubricDocument(
    [property: BsonElement("name")] string Name,
    [property: BsonElement("text")] string Text);
