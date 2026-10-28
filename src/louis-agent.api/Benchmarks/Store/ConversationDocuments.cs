using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Choices;
using MongoDB.Bson;
using static LouisAgent.Api.Benchmarks.Runs.BenchmarkProtocol;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;
using static LouisAgent.Api.Benchmarks.Store.BsonValues;

namespace LouisAgent.Api.Benchmarks.Store;

/// <summary>
/// A conversation as MongoDB keeps it, with its grades and its scores as the JSON they came as, and back. Its id is
/// its run and its key, so saving it again replaces it.
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
        ["scores"] = Bson(c.Scores),
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
    };

    /// <summary>A document turned back into the conversation it was.</summary>
    public static Conversation ConversationOf(BsonDocument d) => new(
        new ConversationKey(d["mode"].AsString, d["repeat"].AsInt32, d["task"].AsString),
        d["order"].AsInt32, StringOf(d["role"]), Json(d["scores"]), d["errors"].AsInt32, d["seconds"].ToDouble(),
        d.GetValue("grades", BsonNull.Value).IsBsonNull
            ? null
            : d["grades"].AsBsonArray.Select(g => GradeOf(g.AsBsonDocument)).ToArray(),
        d.GetValue("outcome", BsonNull.Value).IsBsonNull
            ? null
            : Enum.Parse<BenchmarkOutcome>(d["outcome"].AsString, ignoreCase: true));

    /// <summary>One grader's grade read back, with the judge that gave it when there was one.</summary>
    private static Grade GradeOf(BsonDocument g) => new(g["grader"].AsString, g["version"].AsInt32,
        g["score"].ToDouble(), Enum.Parse<BenchmarkOutcome>(g["outcome"].AsString, ignoreCase: true),
        g["reason"].AsString, StringOf(g.GetValue("judge_model", BsonNull.Value)),
        StringOf(g.GetValue("judge_prompt_hash", BsonNull.Value)));
}
