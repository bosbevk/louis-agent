using LouisAgent.Api.Evals.Reports;
using LouisAgent.Api.Evals.Runs.Kinds;
using LouisAgent.Choices;
using MongoDB.Bson;
using static LouisAgent.Api.Evals.Runs.Protocol.EvalProtocol;
using static LouisAgent.Api.Evals.Store.EvalDocuments;
using static LouisAgent.Api.Evals.Store.Mongo.BsonValues;

namespace LouisAgent.Api.Evals.Store.Mongo.Documents;

/// <summary>
/// A turn as MongoDB keeps it, its stream events inside, and back. Reading a turn leaves the events out (they're the
/// heavy part); they're read on their own when a turn is opened.
/// </summary>
/// <remarks>
/// Data Mapper, written by hand, as the run's is. It was taken out of the Mongo store, which was past 200 lines.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0022-benchmarks-recorded-from-the-protocol.md">ADR 0022: benchmark results in MongoDB</seealso>
internal static class TurnDocuments
{
    /// <summary>The id of a turn's document: its run, its conversation's key and its number.</summary>
    public static BsonDocument Id(string run, TurnKey key) =>
        new()
        {
            ["run"] = run,
            ["mode"] = key.Mode,
            ["repeat"] = key.Repeat,
            ["task"] = key.Task,
            ["turn"] = key.Turn
        };

    /// <summary>A turn as a document, with its stream events inside.</summary>
    public static BsonDocument ToBson(string run, Turn t, IReadOnlyList<StreamEvent> events) => new()
    {
        ["_id"] = Id(run, t.Key),
        ["run"] = run,
        ["mode"] = t.Key.Mode,
        ["repeat"] = t.Key.Repeat,
        ["task"] = t.Key.Task,
        ["turn"] = t.Key.Turn,
        ["session"] = t.Session,
        ["question"] = t.Question,
        ["answer"] = Nullable(t.Answer),
        ["role"] = Nullable(t.Role),
        ["continued"] = Nullable(t.Continued),
        ["puls_calls"] = t.PulsCalls,
        ["specialist_calls"] = t.SpecialistCalls,
        ["hosts"] = new BsonArray(t.Hosts),
        ["seconds"] = t.Seconds,
        ["error"] = Nullable(t.Error),
        ["failure"] = Nullable(t.Failure is { } failure ? failure.Wire() : null),
        ["tools"] = t.Tools is { } tools ? ToBson(tools) : BsonNull.Value,
        ["events"] = new BsonArray(events.Select(e => new BsonDocument
        {
            ["type"] = e.Type,
            ["data"] = Bson(e.Data),
            ["at"] = Date(e.At)
        })),
    };

    /// <summary>A document turned back into the turn it was, without its events.</summary>
    public static Turn TurnOf(BsonDocument d) => new(
        new TurnKey(d["mode"].AsString, d["repeat"].AsInt32, d["task"].AsString, d["turn"].AsInt32),
        d["session"].AsString, d["question"].AsString, StringOf(d["answer"]), StringOf(d["role"]),
        StringOf(d["continued"]),
        d["puls_calls"].AsInt32, d["specialist_calls"].AsInt32,
        d["hosts"].AsBsonArray.Select(h => h.AsString).ToArray(),
        d["seconds"].ToDouble(), StringOf(d["error"]), ToolsOf(d.GetValue("tools", BsonNull.Value)),
        FailureOf(d.GetValue("failure", BsonNull.Value)));

    /// <summary>A stored event turned back into a stream event.</summary>
    public static StreamEvent EventOf(BsonDocument e) => new(e["type"].AsString, Json(e["data"]), DateOf(e["at"]));

    /// <summary>What a turn's tools did: how many calls, how many failed, and why.</summary>
    private static BsonDocument ToBson(EvalTools.Summary tools) => new()
    {
        ["calls"] = tools.Calls,
        ["failed"] = tools.Failed,
        ["reasons"] = new BsonArray(tools.Reasons.Select(r => new BsonDocument
        {
            ["tool"] = r.Tool,
            ["reason"] = r.Reason,
            ["count"] = r.Count
        })),
    };

    /// <summary>A turn's failure; a turn saved before failures were kept has none.</summary>
    private static EvalTurnFailure? FailureOf(BsonValue v) =>
        v is { IsString: true } && SettingChoice.TryParse(v.AsString, out EvalTurnFailure? failure)
            ? failure
            : null;

    /// <summary>
    /// A turn's tool counts; a turn saved before they were kept has none, which is not the same as zero calls.
    /// </summary>
    private static EvalTools.Summary? ToolsOf(BsonValue v) => v is BsonDocument t
        ? new EvalTools.Summary(t["calls"].AsInt32, t["failed"].AsInt32,
            t["reasons"].AsBsonArray.Select(r =>
                new EvalTools.Failure(r["tool"].AsString, r["reason"].AsString, r["count"].AsInt32)).ToArray())
        : null;
}
