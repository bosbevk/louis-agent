using LouisAgent.Choices;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Protocol.Parts.Conversations;
using MongoDB.Bson;
using static LouisAgent.Api.Evals.Store.EvalDocuments;
using static LouisAgent.Api.Evals.Store.Mongo.BsonValues;

namespace LouisAgent.Api.Evals.Store.Mongo.Documents;

/// <summary>
/// A conversation as MongoDB keeps it, with its grades and what it measured, and back. Its id is its run and its key,
/// so saving it again replaces it.
/// </summary>
/// <remarks>
/// Data Mapper, written by hand, as the run's is: nothing the portal reads hides behind a convention. It was taken out
/// of the Mongo store, which was past 200 lines.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0022-benchmarks-recorded-from-the-protocol.md">ADR 0022: benchmark results in MongoDB</seealso>
internal static class ConversationDocuments
{
    /// <summary>The id of a conversation's document: its run and its key.</summary>
    public static BsonDocument Id(string run, ConversationKey key) =>
        new()
        {
            ["run"] = run,
            ["mode"] = key.Mode,
            ["repeat"] = key.Repeat,
            ["task"] = key.Task
        };

    /// <summary>A conversation as a document, tagged with its run.</summary>
    public static BsonDocument ToBson(string run, Conversation c) => new()
    {
        ["_id"] = Id(run, c.Key),
        ["run"] = run,
        ["mode"] = c.Key.Mode,
        ["repeat"] = c.Key.Repeat,
        ["task"] = c.Key.Task,
        ["order"] = c.Order,
        ["role"] = Nullable(c.Role),
        ["metrics"] = c.Metrics is null ? BsonNull.Value : MetricsToBson(c.Metrics),
        ["errors"] = c.Errors,
        ["seconds"] = c.Seconds,
        ["grades"] = c.Grades is null
            ? BsonNull.Value
            : new BsonArray(c.Grades.Select(g => new BsonDocument
            {
                ["grader"] = g.Grader,
                ["version"] = g.Version,
                ["score"] = g.Score,
                ["outcome"] = g.Outcome.Wire(),
                ["reason"] = g.Reason,
                ["judge_model"] = Nullable(g.JudgeModel),
                ["judge_prompt_hash"] = Nullable(g.JudgePromptHash),
            })),
        ["outcome"] = Nullable(c.Outcome?.Wire()),
        ["at"] = Date(c.At),
    };

    /// <summary>A document turned back into the conversation it was.</summary>
    public static Conversation ConversationOf(BsonDocument d) => new(
        new ConversationKey(d["mode"].AsString, d["repeat"].AsInt32, d["task"].AsString),
        d["order"].AsInt32, StringOf(d["role"]), MetricsOf(d.GetValue("metrics", BsonNull.Value)),
        d["errors"].AsInt32, d["seconds"].ToDouble(),
        d.GetValue("grades", BsonNull.Value).IsBsonNull
            ? null
            : d["grades"].AsBsonArray.Select(g => GradeOf(g.AsBsonDocument)).ToArray(),
        d.GetValue("outcome", BsonNull.Value).IsBsonNull ? null : ChoiceOf<TrialOutcome>(d["outcome"], "outcome"),
        DateOf(d.GetValue("at", BsonNull.Value)));

    /// <summary>What a conversation measured, each field null when it wasn't known, never 0.</summary>
    private static BsonDocument MetricsToBson(Metrics m) => new()
    {
        ["agent_calls"] = Nullable(m.AgentCalls),
        ["specialist_calls"] = Nullable(m.SpecialistCalls),
        ["hosts"] = m.Hosts is null ? BsonNull.Value : new BsonArray(m.Hosts),
        ["turns"] = Nullable(m.Turns),
        ["tokens_in"] = Nullable(m.TokensIn),
        ["tokens_out"] = Nullable(m.TokensOut),
        ["seconds"] = Nullable(m.Seconds),
        ["cost"] = Nullable(m.Cost),
    };

    /// <summary>What a conversation measured, read back; none before it ended.</summary>
    private static Metrics? MetricsOf(BsonValue value) =>
        value is not { IsBsonDocument: true }
            ? null
            : new Metrics(IntOf(value["agent_calls"]), IntOf(value["specialist_calls"]),
                value["hosts"].IsBsonNull ? null : value["hosts"].AsBsonArray.Select(h => h.AsString).ToArray(),
                IntOf(value["turns"]), LongOf(value["tokens_in"]), LongOf(value["tokens_out"]),
                DoubleOf(value["seconds"]), DecimalOf(value["cost"]));

    /// <summary>A whole number back, with a null staying unknown.</summary>
    private static int? IntOf(BsonValue value) => value.IsBsonNull ? null : value.AsInt32;

    /// <summary>One grader's grade read back, with the judge that gave it when there was one.</summary>
    private static Grade GradeOf(BsonDocument g) => new(g["grader"].AsString, g["version"].AsInt32,
        g["score"].ToDouble(), ChoiceOf<GradeOutcome>(g["outcome"], "outcome"),
        g["reason"].AsString, StringOf(g.GetValue("judge_model", BsonNull.Value)),
        StringOf(g.GetValue("judge_prompt_hash", BsonNull.Value)));
}
