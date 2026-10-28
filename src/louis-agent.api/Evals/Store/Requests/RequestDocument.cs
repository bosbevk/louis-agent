using LouisAgent.Evals.Protocol.Parts.Conversations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Evals.Store.Requests;

/// <summary>
/// A model request from the usage ledger as MongoDB keeps it in the <c>requests</c> collection, beside its run: its
/// tokens, its cost and the turn it belongs to. A count or cost the ledger didn't know stays null, never 0. It has no
/// id of its own: Mongo gives it one on insert, and nothing looks a request up by it.
/// </summary>
/// <remarks>
/// Metadata Mapping on a Data Transfer Object (Fowler, PoEAA): the record says how each field is stored and the
/// driver does the copying, as <see cref="Runs.RunDocument"/> explains. The properties keep the order the hand mapper
/// wrote, so a document is the same field for field.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0007-measure-every-request.md">ADR 0007: measure every request</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
[BsonIgnoreExtraElements]
internal sealed record RequestDocument(
    [property: BsonElement("run")] string Run,
    [property: BsonElement("turn")] TurnKeyDocument? Turn,
    [property: BsonElement("at")] DateTimeOffset At,
    [property: BsonElement("purpose")] string Purpose,
    [property: BsonElement("session")] string? Session,
    [property: BsonElement("parent")] string? Parent,
    [property: BsonElement("model")] string? Model,
    [property: BsonElement("input_tokens")]
    long? InputTokens,
    [property: BsonElement("output_tokens")]
    long? OutputTokens,
    [property: BsonElement("cache_read_tokens")]
    long? CacheReadTokens,
    [property: BsonElement("cache_write_tokens")]
    long? CacheWriteTokens,
    [property: BsonElement("cost"), BsonRepresentation(BsonType.Decimal128)]
    decimal? Cost,
    [property: BsonElement("duration_ms")] long DurationMs)
{
    /// <summary>The document for a request.</summary>
    public static RequestDocument Of(Request r) =>
        new(r.Run, r.Turn is null ? null : TurnKeyDocument.Of(r.Turn), r.At, r.Purpose, r.Session, r.Parent, r.Model,
            r.InputTokens, r.OutputTokens, r.CacheReadTokens, r.CacheWriteTokens, r.Cost, r.DurationMs);

    /// <summary>The request this document holds.</summary>
    public Request ToRequest() =>
        new(Run, Turn?.ToKey(), At, Purpose, Session, Parent, Model, InputTokens, OutputTokens, CacheReadTokens,
            CacheWriteTokens, Cost, DurationMs);
}

/// <summary>The turn a request belongs to: its conversation's key and its number.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
[BsonIgnoreExtraElements]
internal sealed record TurnKeyDocument(
    [property: BsonElement("mode")] string Mode,
    [property: BsonElement("repeat")] int Repeat,
    [property: BsonElement("task")] string Task,
    [property: BsonElement("turn")] int Turn)
{
    /// <summary>The document for a turn's key.</summary>
    public static TurnKeyDocument Of(TurnKey key) => new(key.Mode, key.Repeat, key.Task, key.Turn);

    /// <summary>The turn's key this document holds.</summary>
    public TurnKey ToKey() => new(Mode, Repeat, Task, Turn);
}
