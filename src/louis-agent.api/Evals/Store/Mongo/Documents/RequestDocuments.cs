using MongoDB.Bson;
using LouisAgent.Evals.Protocol.Parts.Conversations;
using static LouisAgent.Api.Evals.Store.EvalDocuments;
using static LouisAgent.Api.Evals.Store.Mongo.BsonValues;

namespace LouisAgent.Api.Evals.Store.Mongo.Documents;

/// <summary>
/// A model request from the usage ledger as MongoDB keeps it beside its run, and back: its tokens, its cost and the
/// turn it belongs to. A cost the price table didn't know stays null, never 0.
/// </summary>
/// <remarks>
/// Data Mapper, written by hand, as the run's is. It was taken out of the Mongo store, which was past 200 lines.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0007-measure-every-request.md">ADR 0007: measure every request</seealso>
internal static class RequestDocuments
{
    /// <summary>A request as a document.</summary>
    public static BsonDocument ToBson(Request r) => new()
    {
        ["run"] = r.Run,
        ["turn"] = r.Turn is null
            ? BsonNull.Value
            : new BsonDocument
            {
                ["mode"] = r.Turn.Mode,
                ["repeat"] = r.Turn.Repeat,
                ["task"] = r.Turn.Task,
                ["turn"] = r.Turn.Turn
            },
        ["at"] = Date(r.At),
        ["purpose"] = r.Purpose,
        ["session"] = Nullable(r.Session),
        ["parent"] = Nullable(r.Parent),
        ["model"] = Nullable(r.Model),
        ["input_tokens"] = Nullable(r.InputTokens),
        ["output_tokens"] = Nullable(r.OutputTokens),
        ["cache_read_tokens"] = Nullable(r.CacheReadTokens),
        ["cache_write_tokens"] = Nullable(r.CacheWriteTokens),
        ["cost"] = Nullable(r.Cost),
        ["duration_ms"] = r.DurationMs,
    };

    /// <summary>A document turned back into the request it was.</summary>
    public static Request RequestOf(BsonDocument d) => new(
        d["run"].AsString,
        d["turn"].IsBsonNull
            ? null
            : new TurnKey(d["turn"]["mode"].AsString, d["turn"]["repeat"].AsInt32, d["turn"]["task"].AsString,
                d["turn"]["turn"].AsInt32),
        DateOf(d["at"])!.Value, d["purpose"].AsString, StringOf(d["session"]), StringOf(d["parent"]),
        StringOf(d["model"]),
        LongOf(d["input_tokens"]), LongOf(d["output_tokens"]), LongOf(d["cache_read_tokens"]),
        LongOf(d["cache_write_tokens"]),
        DecimalOf(d["cost"]), d["duration_ms"].ToInt64());
}
