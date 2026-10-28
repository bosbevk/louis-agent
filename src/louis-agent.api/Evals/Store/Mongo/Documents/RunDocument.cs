using LouisAgent.Api.Evals.Runs.Kinds;
using LouisAgent.Evals.Protocol.Kinds;
using MongoDB.Bson.Serialization.Attributes;
using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Evals.Store.Mongo.Documents;

/// <summary>
/// A run as MongoDB keeps it in the <c>runs</c> collection: one document per run, its modes, total, graders, models,
/// slices, prompts and dataset inside it. The stored field names read off the properties, and a field added later
/// never breaks a read.
/// </summary>
/// <remarks>
/// Metadata Mapping on a Data Transfer Object (Fowler, PoEAA): the record says how each field is stored, and the
/// driver does the copying, so the shape is one place a reader sees whole instead of a mapper written out twice.
/// <see cref="Of"/> and <see cref="ToRun"/> (Replace Constructor with Factory Function) are its only code; the
/// properties keep the order the hand mapper wrote, so a document is the same field for field.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0022-benchmarks-recorded-from-the-protocol.md">ADR 0022: benchmark results in MongoDB</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
[BsonIgnoreExtraElements]
internal sealed record RunDocument(
    [property: BsonId] string Id,
    [property: BsonElement("check")] string Check,
    [property: BsonElement("repeat")] int Repeat,
    [property: BsonElement("status")] EvalRunStatus Status,
    [property: BsonElement("started_at")] DateTimeOffset StartedAt,
    [property: BsonElement("ended_at")] DateTimeOffset? EndedAt,
    [property: BsonElement("exit_code")] int? ExitCode,
    [property: BsonElement("settings")] Dictionary<string, string>? Settings,
    [property: BsonElement("modes")] IReadOnlyList<ModeDocument> Modes,
    [property: BsonElement("total")] TotalDocument? Total,
    [property: BsonElement("log")] IReadOnlyList<string>? Log,
    [property: BsonElement("store_error")] string? StoreError,
    [property: BsonElement("eval")] string? Eval,
    [property: BsonElement("eval_version")]
    int? EvalVersion,
    [property: BsonElement("commit")] string? Commit,
    [property: BsonElement("graders")] IReadOnlyList<GraderDocument>? Graders,
    [property: BsonElement("models")] IReadOnlyList<ModelDocument>? Models,
    [property: BsonElement("split")] RunSplit? Split,
    [property: BsonElement("rescore_of")] string? RescoreOf,
    [property: BsonElement("slices")] IReadOnlyList<SliceDocument>? Slices,
    [property: BsonElement("prompts")] IReadOnlyList<PromptDocument>? Prompts,
    [property: BsonElement("protocol_version")]
    int? ProtocolVersion,
    [property: BsonElement("dataset")] DatasetDocument? Dataset,
    [property: BsonElement("begun_at")] DateTimeOffset? BegunAt)
{
    /// <summary>The document for a run; a run with no modes yet is stored with an empty list of them.</summary>
    public static RunDocument Of(Run run) =>
        new(run.Id, run.Check, run.Repeat, run.Status, run.StartedAt, run.EndedAt, run.ExitCode,
            run.Settings?.ToDictionary(), [.. (run.Modes ?? []).Select(ModeDocument.Of)],
            run.Total is null ? null : TotalDocument.Of(run.Total), run.Log, run.StoreError, run.Eval,
            run.EvalVersion, run.Commit, run.Graders?.Select(GraderDocument.Of).ToArray(),
            run.Models?.Select(ModelDocument.Of).ToArray(), run.Split, run.RescoreOf,
            run.Slices?.Select(SliceDocument.Of).ToArray(), run.Prompts?.Select(PromptDocument.Of).ToArray(),
            run.ProtocolVersion, run.Dataset is null ? null : DatasetDocument.Of(run.Dataset), run.BegunAt);

    /// <summary>The run this document holds.</summary>
    public Run ToRun() =>
        new(Id, Check, Repeat, Status, StartedAt, EndedAt, ExitCode, Settings,
            [.. Modes.Select(m => m.ToMode())], Total?.ToTotal(), Log, StoreError, Eval, EvalVersion, Commit,
            Graders?.Select(g => g.ToGrader()).ToArray(), Models?.Select(m => m.ToModelUse()).ToArray(), Split,
            RescoreOf, Slices?.Select(s => s.ToSlice()).ToArray(), Prompts?.Select(p => p.ToPrompt()).ToArray(),
            ProtocolVersion, Dataset?.ToDataset(), BegunAt);
}
