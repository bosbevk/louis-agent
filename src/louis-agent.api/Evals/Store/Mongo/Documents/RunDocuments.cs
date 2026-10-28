using LouisAgent.Api.Evals.Runs.Kinds;
using LouisAgent.Choices;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Protocol.Parts.Run;
using MongoDB.Bson;
using static LouisAgent.Api.Evals.Store.EvalDocuments;
using static LouisAgent.Api.Evals.Store.Mongo.BsonValues;

namespace LouisAgent.Api.Evals.Store.Mongo.Documents;

/// <summary>
/// A run as MongoDB keeps it, field by field in snake case, and back, with the protocol version it was recorded from.
/// A field added later is absent in a run stored before it and reads as null.
/// </summary>
/// <remarks>
/// Data Mapper, written by hand: what <c>mongosh</c> shows is exactly what the portal reads, with no convention in
/// between. It was taken out of the Mongo store, which was past 200 lines.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0022-benchmarks-recorded-from-the-protocol.md">ADR 0022: benchmark results in MongoDB</seealso>
internal static class RunDocuments
{
    /// <summary>A run as a Mongo document.</summary>
    public static BsonDocument ToBson(Run run) => new()
    {
        ["_id"] = run.Id,
        ["check"] = run.Check,
        ["repeat"] = run.Repeat,
        ["status"] = Status(run.Status),
        ["started_at"] = Date(run.StartedAt),
        ["ended_at"] = Date(run.EndedAt),
        ["exit_code"] = Nullable(run.ExitCode),
        ["settings"] = run.Settings is null
            ? BsonNull.Value
            : new BsonDocument(run.Settings.ToDictionary(s => s.Key, s => (object)s.Value)),
        ["modes"] = new BsonArray((run.Modes ?? []).Select(ToBson)),
        ["total"] = run.Total is null
            ? BsonNull.Value
            : new BsonDocument
            {
                ["cost"] = Nullable(run.Total.Cost),
                ["currency"] = run.Total.Currency,
                ["tokens"] = run.Total.Tokens,
                ["requests"] = run.Total.Requests,
            },
        ["log"] = run.Log is null ? BsonNull.Value : new BsonArray(run.Log),
        ["store_error"] = Nullable(run.StoreError),
        // What the run ran: the eval, its version and the commit.
        ["eval"] = Nullable(run.Eval),
        ["eval_version"] = Nullable(run.EvalVersion),
        ["commit"] = Nullable(run.Commit),
        ["graders"] = run.Graders is null
            ? BsonNull.Value
            : new BsonArray(run.Graders.Select(g => new BsonDocument
            {
                ["name"] = g.Name,
                ["version"] = g.Version,
                ["kind"] = g.Kind.Wire(),
                ["question"] = Nullable(g.Question),
            })),
        ["models"] = run.Models is null
            ? BsonNull.Value
            : new BsonArray(run.Models.Select(m => new BsonDocument
            {
                ["mode"] = m.Mode,
                ["level"] = m.Level.Wire(),
                ["role"] = Nullable(m.Role),
                ["model"] = Nullable(m.Model),
            })),
        // The scenarios the run asked for, the run it re-scored, its roll-up and each agent's prompts per mode.
        ["split"] = Nullable(run.Split is { } split ? split.Wire() : null),
        ["rescore_of"] = Nullable(run.RescoreOf),
        ["slices"] = SliceDocuments.ToBson(run.Slices),
        ["prompts"] = PromptDocuments.ToBson(run.Prompts),
        // The protocol version the run was recorded from: a store drops the runs of an older one when it opens.
        ["protocol_version"] = Nullable(run.ProtocolVersion),
        // The dataset and the first line's time, so an export gives the first line back as it came.
        ["dataset"] = run.Dataset is { } dataset
            ? new BsonDocument
            {
                ["name"] = dataset.Name,
                ["version"] = dataset.Version,
                ["hash"] = dataset.Hash
            }
            : BsonNull.Value,
        ["begun_at"] = Date(run.BegunAt),
    };

    /// <summary>
    /// A run read back from its document. A mode saved when roles were still called personalities keeps that field,
    /// read as its role and never rewritten.
    /// </summary>
    public static Run RunOf(BsonDocument d) => new(
        d["_id"].AsString,
        d["check"].AsString,
        d["repeat"].AsInt32,
        ChoiceOf<EvalRunStatus>(d["status"], "status"),
        DateOf(d["started_at"])!.Value,
        DateOf(d["ended_at"]),
        d["exit_code"].IsBsonNull ? null : d["exit_code"].AsInt32,
        d["settings"].IsBsonNull
            ? null
            : d["settings"].AsBsonDocument.ToDictionary(e => e.Name, e => e.Value.AsString),
        d["modes"].AsBsonArray.Select(m => ModeOf(m.AsBsonDocument)).ToArray(),
        d["total"].IsBsonNull
            ? null
            : new Total(DecimalOf(d["total"]["cost"]), d["total"]["currency"].AsString,
                d["total"]["tokens"].ToInt64(), d["total"]["requests"].AsInt32),
        d["log"].IsBsonNull ? null : d["log"].AsBsonArray.Select(l => l.AsString).ToArray(),
        StringOf(d["store_error"]),
        StringOf(d.GetValue("eval", BsonNull.Value)),
        d.GetValue("eval_version", BsonNull.Value).IsBsonNull ? null : d["eval_version"].AsInt32,
        StringOf(d.GetValue("commit", BsonNull.Value)),
        d.GetValue("graders", BsonNull.Value).IsBsonNull
            ? null
            : d["graders"].AsBsonArray.Select(g => g.AsBsonDocument)
                .Select(g => new Grader(g["name"].AsString, g["version"].AsInt32,
                    ChoiceOf<GraderKind>(g["kind"], "kind"), StringOf(g.GetValue("question", BsonNull.Value))))
                .ToArray(),
        d.GetValue("models", BsonNull.Value).IsBsonNull
            ? null
            : d["models"].AsBsonArray.Select(m => m.AsBsonDocument)
                .Select(m => new ModelUse(m["mode"].AsString, ChoiceOf<ModelLevel>(m["level"], "level"),
                    StringOf(m.GetValue("role", BsonNull.Value)), StringOf(m.GetValue("model", BsonNull.Value))))
                .ToArray(),
        SplitOf(d.GetValue("split", BsonNull.Value)),
        StringOf(d.GetValue("rescore_of", BsonNull.Value)), SliceDocuments.Of(d.GetValue("slices", BsonNull.Value)),
        PromptDocuments.Of(d.GetValue("prompts", BsonNull.Value)),
        d.GetValue("protocol_version", BsonNull.Value).IsBsonNull ? null : d["protocol_version"].AsInt32,
        DatasetOf(d.GetValue("dataset", BsonNull.Value)),
        DateOf(d.GetValue("begun_at", BsonNull.Value)));

    /// <summary>A run's status as the text we store.</summary>
    public static string Status(EvalRunStatus status) => status.Wire();

    /// <summary>
    /// One mode of the run, with how many conversations it planned, when it started and ended, and its pass rates
    /// when it has them.
    /// </summary>
    private static BsonDocument ToBson(Mode m) => new()
    {
        ["name"] = m.Name,
        ["repeat"] = m.Repeat,
        ["toolsets"] = new BsonArray(m.Toolsets),
        ["role"] = Nullable(m.Role),
        ["llm"] = Nullable(m.Llm),
        ["conversations"] = Nullable(m.Conversations),
        ["at"] = Date(m.At),
        ["done_at"] = Date(m.DoneAt),
        ["rates"] = m.Rates is { } r
            ? new BsonDocument
            {
                ["pass_at_k"] = Nullable(r.PassAtK),
                ["pass_hat_k"] = Nullable(r.PassHatK),
                ["k"] = r.K,
                ["infrastructure"] = r.Infrastructure,
                ["retried"] = r.Retried
            }
            : BsonNull.Value,
    };

    /// <summary>One mode read back; a mode saved when roles were personalities reads that field as its role.</summary>
    private static Mode ModeOf(BsonDocument m) => new(m["name"].AsString,
        m["repeat"].AsInt32,
        m["toolsets"].AsBsonArray.Select(t => t.AsString).ToArray(),
        StringOf(m.GetValue("role", null) ?? m.GetValue("personality", BsonNull.Value)), StringOf(m["llm"]),
        RatesOf(m.GetValue("rates", BsonNull.Value)),
        m.GetValue("conversations", BsonNull.Value).IsBsonNull ? null : m["conversations"].AsInt32,
        DateOf(m.GetValue("at", BsonNull.Value)), DateOf(m.GetValue("done_at", BsonNull.Value)));

    /// <summary>A mode's pass rates; a mode saved before they existed has none.</summary>
    private static ModeRates? RatesOf(BsonValue v)
    {
        if (v is not BsonDocument rates) return null;
        return new ModeRates(DoubleOf(rates["pass_at_k"]), DoubleOf(rates["pass_hat_k"]), rates["k"].AsInt32,
            rates["infrastructure"].AsInt32, rates["retried"].AsInt32);
    }

    /// <summary>The dataset the run's scenarios came from; a run without one has none.</summary>
    private static DatasetInfo? DatasetOf(BsonValue v) =>
        v is BsonDocument d ? new DatasetInfo(d["name"].AsString, d["version"].AsInt32, d["hash"].AsString) : null;

    /// <summary>A run's split; a run saved before it was kept has none.</summary>
    private static RunSplit? SplitOf(BsonValue v) =>
        v is { IsString: true } && SettingChoice.TryParse(v.AsString, out RunSplit? split) ? split : null;
}
