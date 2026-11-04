using System.Text.Json;
using LouisAgent.Api.Evals.Reports;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Protocol.Parts.Conversations;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Attributes;
using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Evals.Store.Turns;

/// <summary>
/// A turn as MongoDB keeps it in the <c>turns</c> collection, its stream events inside. Its id is its run and its key,
/// so saving it again replaces it and its events. Reading a conversation's turns leaves the events out (they're the
/// heavy part); they're read on their own when a turn is opened. A field added later never breaks a read.
/// </summary>
/// <remarks>
/// Metadata Mapping on a Data Transfer Object (Fowler, PoEAA): the record says how each field is stored and the
/// driver does the copying, as <see cref="Runs.RunDocument"/> explains. The properties keep the order the hand mapper
/// wrote, so a document is the same field for field. The events may be missing when a query leaves them out.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0022-benchmarks-recorded-from-the-protocol.md">ADR 0022: benchmark results in MongoDB</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
[BsonIgnoreExtraElements]
internal sealed record TurnDocument(
    [property: BsonId] TurnIdDocument Id,
    [property: BsonElement("run")] string Run,
    [property: BsonElement("mode")] string Mode,
    [property: BsonElement("repeat")] int Repeat,
    [property: BsonElement("task")] string Task,
    [property: BsonElement("turn")] int Turn,
    [property: BsonElement("session")] string Session,
    [property: BsonElement("question")] string Question,
    [property: BsonElement("answer")] string? Answer,
    [property: BsonElement("role")] string? Role,
    [property: BsonElement("continued")] string? Continued,
    [property: BsonElement("agent_calls")] int AgentCalls,
    [property: BsonElement("specialist_calls")]
    int SpecialistCalls,
    [property: BsonElement("hosts")] IReadOnlyList<string> Hosts,
    [property: BsonElement("seconds")] double Seconds,
    [property: BsonElement("error")] string? Error,
    [property: BsonElement("failure")] TurnFailure? Failure,
    [property: BsonElement("tools")] ToolsDocument? Tools,
    [property: BsonElement("asked_at")] DateTimeOffset? AskedAt,
    [property: BsonElement("ended_at")] DateTimeOffset? EndedAt,
    [property: BsonElement("events")] IReadOnlyList<EventDocument>? Events)
{
    /// <summary>The document for a run's turn, with its stream events.</summary>
    public static TurnDocument Of(string run, Turn t, IReadOnlyList<StreamEvent> events) =>
        new(TurnIdDocument.Of(run, t.Key), run, t.Key.Mode, t.Key.Repeat, t.Key.Task, t.Key.Turn, t.Session,
            t.Question, t.Answer, t.Role, t.Continued, t.AgentCalls, t.SpecialistCalls, t.Hosts, t.Seconds, t.Error,
            t.Failure, t.Tools is null ? null : ToolsDocument.Of(t.Tools), t.AskedAt, t.EndedAt,
            [.. events.Select(EventDocument.Of)]);

    /// <summary>The turn this document holds, without its events.</summary>
    public Turn ToTurn() =>
        new(new TurnKey(Mode, Repeat, Task, Turn), Session, Question, Answer, Role, Continued, AgentCalls,
            SpecialistCalls, Hosts, Seconds, Error, Tools?.ToSummary(), Failure, AskedAt, EndedAt);

    /// <summary>The turn's stream events in the order they came; none when the query left them out.</summary>
    public IReadOnlyList<StreamEvent> ToEvents() => [.. (Events ?? []).Select(e => e.ToEvent())];
}

/// <summary>A turn's id: the run it belongs to, its conversation's key and its number.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
internal sealed record TurnIdDocument(
    [property: BsonElement("run")] string Run,
    [property: BsonElement("mode")] string Mode,
    [property: BsonElement("repeat")] int Repeat,
    [property: BsonElement("task")] string Task,
    [property: BsonElement("turn")] int Turn)
{
    /// <summary>The id of a run's turn with this key.</summary>
    public static TurnIdDocument Of(string run, TurnKey key) => new(run, key.Mode, key.Repeat, key.Task, key.Turn);
}

/// <summary>What a turn's tools did: how many calls, how many failed, and why.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
[BsonIgnoreExtraElements]
internal sealed record ToolsDocument(
    [property: BsonElement("calls")] int Calls,
    [property: BsonElement("failed")] int Failed,
    [property: BsonElement("reasons")] IReadOnlyList<ToolFailureDocument> Reasons)
{
    /// <summary>The document for what a turn's tools did.</summary>
    public static ToolsDocument Of(EvalTools.Summary s) =>
        new(s.Calls, s.Failed, [.. s.Reasons.Select(ToolFailureDocument.Of)]);

    /// <summary>What the tools did, as this document holds it.</summary>
    public EvalTools.Summary ToSummary() => new(Calls, Failed, [.. Reasons.Select(r => r.ToFailure())]);
}

/// <summary>One reason a tool failed in a turn, and how often.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
[BsonIgnoreExtraElements]
internal sealed record ToolFailureDocument(
    [property: BsonElement("tool")] string Tool,
    [property: BsonElement("reason")] string Reason,
    [property: BsonElement("count")] int Count)
{
    /// <summary>The document for a tool's failure.</summary>
    public static ToolFailureDocument Of(EvalTools.Failure f) => new(f.Tool, f.Reason, f.Count);

    /// <summary>The tool's failure this document holds.</summary>
    public EvalTools.Failure ToFailure() => new(Tool, Reason, Count);
}

/// <summary>
/// One event from a turn's stream, kept so the turn can be replayed. Its data is whatever JSON the stream sent, so it
/// is held as BSON as it came and turned back into the same JSON.
/// </summary>
/// <remarks>
/// ADR 0048 keeps a shape decided at run time out of the record's typed fields: the data is a <see cref="BsonValue"/>
/// property, and only it.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
[BsonIgnoreExtraElements]
internal sealed record EventDocument(
    [property: BsonElement("type")] string Type,
    [property: BsonElement("data")] BsonValue Data,
    [property: BsonElement("at")] DateTimeOffset? At)
{
    /// <summary>How BSON is written back out as JSON.</summary>
    private static readonly JsonWriterSettings RelaxedJson = new() { OutputMode = JsonOutputMode.RelaxedExtendedJson };

    /// <summary>The document for a stream event.</summary>
    public static EventDocument Of(StreamEvent e) => new(e.Type, Bson(e.Data), e.At);

    /// <summary>The stream event this document holds.</summary>
    public StreamEvent ToEvent() => new(Type, Json(Data), At);

    /// <summary>
    /// Any JSON value as BSON. It's wrapped in an object and unwrapped again, because BSON's root must be a document.
    /// </summary>
    private static BsonValue Bson(JsonElement json) => BsonDocument.Parse($$"""{"v":{{json.GetRawText()}}}""")["v"];

    /// <summary>A BSON value turned back into the JSON it came as.</summary>
    private static JsonElement Json(BsonValue bson)
    {
        // Wrapped the same way as on the way in, then the wrapper is read off again.
        using var document = JsonDocument.Parse(new BsonDocument("v", bson).ToJson(RelaxedJson));
        return document.RootElement.GetProperty("v").Clone();
    }
}
