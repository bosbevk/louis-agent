using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Protocol.Parts.Conversations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Evals.Store.Mongo.Documents;

/// <summary>
/// A conversation as MongoDB keeps it in the <c>conversations</c> collection, with its grades and what it measured.
/// Its id is its run and its key, so saving it again replaces it. A field added later leaves older documents
/// readable: what they never stored comes back empty.
/// </summary>
/// <remarks>
/// Metadata Mapping on a Data Transfer Object (Fowler, PoEAA): the record says how each field is stored and the
/// driver does the copying, as <see cref="RunDocument"/> explains. The properties keep the order the hand mapper
/// wrote, so a document is the same field for field.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0022-benchmarks-recorded-from-the-protocol.md">ADR 0022: benchmark results in MongoDB</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
[BsonIgnoreExtraElements]
internal sealed record ConversationDocument(
    [property: BsonId] ConversationIdDocument Id,
    [property: BsonElement("run")] string Run,
    [property: BsonElement("mode")] string Mode,
    [property: BsonElement("repeat")] int Repeat,
    [property: BsonElement("task")] string Task,
    [property: BsonElement("order")] int Order,
    [property: BsonElement("role")] string? Role,
    [property: BsonElement("metrics")] MetricsDocument? Metrics,
    [property: BsonElement("errors")] int Errors,
    [property: BsonElement("seconds")] double Seconds,
    [property: BsonElement("grades")] IReadOnlyList<GradeDocument>? Grades,
    [property: BsonElement("outcome")] TrialOutcome? Outcome,
    [property: BsonElement("at")] DateTimeOffset? At)
{
    /// <summary>The document for a run's conversation.</summary>
    public static ConversationDocument Of(string run, Conversation c) =>
        new(ConversationIdDocument.Of(run, c.Key), run, c.Key.Mode, c.Key.Repeat, c.Key.Task, c.Order, c.Role,
            c.Metrics is null ? null : MetricsDocument.Of(c.Metrics), c.Errors, c.Seconds,
            c.Grades?.Select(GradeDocument.Of).ToArray(), c.Outcome, c.At);

    /// <summary>The conversation this document holds.</summary>
    public Conversation ToConversation() =>
        new(new ConversationKey(Mode, Repeat, Task), Order, Role, Metrics?.ToMetrics(), Errors, Seconds,
            Grades?.Select(g => g.ToGrade()).ToArray(), Outcome, At);
}

/// <summary>A conversation's id: the run it belongs to and its key.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
internal sealed record ConversationIdDocument(
    [property: BsonElement("run")] string Run,
    [property: BsonElement("mode")] string Mode,
    [property: BsonElement("repeat")] int Repeat,
    [property: BsonElement("task")] string Task)
{
    /// <summary>The id of a run's conversation with this key.</summary>
    public static ConversationIdDocument Of(string run, ConversationKey key) =>
        new(run, key.Mode, key.Repeat, key.Task);
}

/// <summary>What a conversation measured; a field it didn't know stays null, never 0.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
[BsonIgnoreExtraElements]
internal sealed record MetricsDocument(
    [property: BsonElement("agent_calls")] int? AgentCalls,
    [property: BsonElement("specialist_calls")]
    int? SpecialistCalls,
    [property: BsonElement("hosts")] IReadOnlyList<string>? Hosts,
    [property: BsonElement("turns")] int? Turns,
    [property: BsonElement("tokens_in")] long? TokensIn,
    [property: BsonElement("tokens_out")] long? TokensOut,
    [property: BsonElement("seconds")] double? Seconds,
    [property: BsonElement("cost"), BsonRepresentation(BsonType.Decimal128)]
    decimal? Cost)
{
    /// <summary>The document for what a conversation measured.</summary>
    public static MetricsDocument Of(Metrics m) =>
        new(m.AgentCalls, m.SpecialistCalls, m.Hosts, m.Turns, m.TokensIn, m.TokensOut, m.Seconds, m.Cost);

    /// <summary>What the conversation measured, as this document holds it.</summary>
    public Metrics ToMetrics() => new(AgentCalls, SpecialistCalls, Hosts, Turns, TokensIn, TokensOut, Seconds, Cost);
}

/// <summary>One grader's grade on a conversation, with the judge model and prompt when a model gave it.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
[BsonIgnoreExtraElements]
internal sealed record GradeDocument(
    [property: BsonElement("grader")] string Grader,
    [property: BsonElement("version")] int Version,
    [property: BsonElement("score")] double Score,
    [property: BsonElement("outcome")] GradeOutcome Outcome,
    [property: BsonElement("reason")] string Reason,
    [property: BsonElement("judge_model")] string? JudgeModel,
    [property: BsonElement("judge_prompt_hash")]
    string? JudgePromptHash)
{
    /// <summary>The document for a grade.</summary>
    public static GradeDocument Of(Grade g) =>
        new(g.Grader, g.Version, g.Score, g.Outcome, g.Reason, g.JudgeModel, g.JudgePromptHash);

    /// <summary>The grade this document holds.</summary>
    public Grade ToGrade() => new(Grader, Version, Score, Outcome, Reason, JudgeModel, JudgePromptHash);
}
