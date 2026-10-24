using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Reports;
using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;
using static LouisAgent.Api.Benchmarks.Runs.BenchmarkProtocol;

namespace LouisAgent.Api.Benchmarks.Store;

/// <summary>
/// The benchmark store in MongoDB (F31): <c>puls-mongo</c>'s <c>puls_benchmarks</c>, one collection per record
/// (<c>runs</c>, <c>conversations</c>, <c>turns</c> with their events, <c>requests</c>). The documents are mapped by
/// hand, field by field in snake case, so what <c>mongosh</c> shows is what the portal reads, and a check's own
/// scores and a turn's stream events are kept as the JSON they came as. The indexes are made on first use. A server
/// that doesn't answer fails a call within <see cref="Timeout"/>, so the recorder gives up quickly and the run goes on.
/// </summary>
public sealed class MongoBenchmarkStore : IBenchmarkStore
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private readonly IMongoCollection<BsonDocument> _runs;
    private readonly IMongoCollection<BsonDocument> _conversations;
    private readonly IMongoCollection<BsonDocument> _turns;
    private readonly IMongoCollection<BsonDocument> _requests;
    private readonly OnceItWorks _indexes;

    public MongoBenchmarkStore(string url, string database)
    {
        var settings = MongoClientSettings.FromConnectionString(url);
        settings.ServerSelectionTimeout = Timeout;
        settings.ConnectTimeout = Timeout;
        var db = new MongoClient(settings).GetDatabase(database);
        _runs = db.GetCollection<BsonDocument>("runs");
        _conversations = db.GetCollection<BsonDocument>("conversations");
        _turns = db.GetCollection<BsonDocument>("turns");
        _requests = db.GetCollection<BsonDocument>("requests");
        _indexes = new OnceItWorks(CreateIndexesAsync);
    }

    /// <summary>The database itself, for the tests to drop theirs.</summary>
    internal IMongoDatabase Database => _runs.Database;

    private async Task CreateIndexesAsync()
    {
        await _runs.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("check").Descending("started_at")));
        await _conversations.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("run").Ascending("order")));
        await _turns.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("run").Ascending("mode").Ascending("repeat").Ascending("task").Ascending("turn")));
        await _requests.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("run").Ascending("at")));
    }

    private Task Ready() => _indexes.RunAsync();

    private static readonly ReplaceOptions Upsert = new() { IsUpsert = true };

    private static FilterDefinition<BsonDocument> ById(BsonValue id) => Builders<BsonDocument>.Filter.Eq("_id", id);

    private static FilterDefinition<BsonDocument> OfRun(string run) => Builders<BsonDocument>.Filter.Eq("run", run);

    public async Task SaveRunAsync(Run run, CancellationToken cancellationToken = default)
    {
        await Ready();
        await _runs.ReplaceOneAsync(ById(run.Id), ToBson(run), Upsert, cancellationToken);
    }

    public async Task<Run?> GetRunAsync(string id, CancellationToken cancellationToken = default)
    {
        await Ready();
        var found = await _runs.Find(ById(id)).FirstOrDefaultAsync(cancellationToken);
        return found is null ? null : RunOf(found);
    }

    public async Task<IReadOnlyList<Run>> ListRunsAsync(string? check = null, int limit = 50, CancellationToken cancellationToken = default)
    {
        await Ready();
        var filter = check is null ? Builders<BsonDocument>.Filter.Empty : Builders<BsonDocument>.Filter.Eq("check", check);
        var found = await _runs.Find(filter).Sort(Builders<BsonDocument>.Sort.Descending("started_at")).Limit(limit)
            .ToListAsync(cancellationToken);
        return found.Select(RunOf).ToArray();
    }

    public async Task SaveConversationAsync(string run, Conversation conversation, CancellationToken cancellationToken = default)
    {
        await Ready();
        await _conversations.ReplaceOneAsync(ById(ConversationId(run, conversation.Key)), ToBson(run, conversation), Upsert, cancellationToken);
    }

    public async Task<IReadOnlyList<Conversation>> ConversationsAsync(string run, CancellationToken cancellationToken = default)
    {
        await Ready();
        var found = await _conversations.Find(OfRun(run)).Sort(Builders<BsonDocument>.Sort.Ascending("order")).ToListAsync(cancellationToken);
        return found.Select(ConversationOf).ToArray();
    }

    public async Task SaveTurnAsync(string run, Turn turn, IReadOnlyList<StreamEvent> events, CancellationToken cancellationToken = default)
    {
        await Ready();
        await _turns.ReplaceOneAsync(ById(TurnId(run, turn.Key)), ToBson(run, turn, events), Upsert, cancellationToken);
    }

    public async Task<IReadOnlyList<Turn>> TurnsAsync(string run, ConversationKey conversation, CancellationToken cancellationToken = default)
    {
        await Ready();
        var filter = Builders<BsonDocument>.Filter.And(OfRun(run), Builders<BsonDocument>.Filter.Eq("mode", conversation.Mode),
            Builders<BsonDocument>.Filter.Eq("repeat", conversation.Repeat), Builders<BsonDocument>.Filter.Eq("task", conversation.Task));
        var found = await _turns.Find(filter).Project<BsonDocument>(Builders<BsonDocument>.Projection.Exclude("events"))
            .Sort(Builders<BsonDocument>.Sort.Ascending("turn")).ToListAsync(cancellationToken);
        return found.Select(TurnOf).ToArray();
    }

    public async Task<IReadOnlyList<StreamEvent>> TurnEventsAsync(string run, TurnKey turn, CancellationToken cancellationToken = default)
    {
        await Ready();
        var found = await _turns.Find(ById(TurnId(run, turn))).Project<BsonDocument>(Builders<BsonDocument>.Projection.Include("events"))
            .FirstOrDefaultAsync(cancellationToken);
        return found?["events"].AsBsonArray.Select(e => EventOf(e.AsBsonDocument)).ToArray() ?? [];
    }

    public async Task AddRequestsAsync(string run, IReadOnlyList<Request> requests, CancellationToken cancellationToken = default)
    {
        if (requests.Count == 0) return;
        await Ready();
        await _requests.InsertManyAsync(requests.Select(r => ToBson(r with { Run = run })), cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<Request>> RequestsAsync(string run, CancellationToken cancellationToken = default)
    {
        await Ready();
        var found = await _requests.Find(OfRun(run)).Sort(Builders<BsonDocument>.Sort.Ascending("at")).ToListAsync(cancellationToken);
        return found.Select(RequestOf).ToArray();
    }

    public async Task<int> CloseOrphansAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        await Ready();
        var result = await _runs.UpdateManyAsync(Builders<BsonDocument>.Filter.Eq("status", Status(BenchmarkRunStatus.Running)),
            Builders<BsonDocument>.Update.Set("status", Status(BenchmarkRunStatus.Failed)).Set("ended_at", Date(now)),
            cancellationToken: cancellationToken);
        return (int)result.ModifiedCount;
    }

    public async Task<bool> DeleteRunAsync(string id, CancellationToken cancellationToken = default)
    {
        await Ready();
        long deleted = (await _runs.DeleteOneAsync(ById(id), cancellationToken)).DeletedCount;
        deleted += (await _conversations.DeleteManyAsync(OfRun(id), cancellationToken)).DeletedCount;
        deleted += (await _turns.DeleteManyAsync(OfRun(id), cancellationToken)).DeletedCount;
        deleted += (await _requests.DeleteManyAsync(OfRun(id), cancellationToken)).DeletedCount;
        return deleted > 0;
    }

    // The documents, field by field.

    private static BsonDocument ConversationId(string run, ConversationKey key) =>
        new() { ["run"] = run, ["mode"] = key.Mode, ["repeat"] = key.Repeat, ["task"] = key.Task };

    private static BsonDocument TurnId(string run, TurnKey key) =>
        new() { ["run"] = run, ["mode"] = key.Mode, ["repeat"] = key.Repeat, ["task"] = key.Task, ["turn"] = key.Turn };

    internal static BsonDocument ToBson(Run run) => new()
    {
        ["_id"] = run.Id,
        ["check"] = run.Check,
        ["repeat"] = run.Repeat,
        ["status"] = Status(run.Status),
        ["started_at"] = Date(run.StartedAt),
        ["ended_at"] = Date(run.EndedAt),
        ["exit_code"] = Nullable(run.ExitCode),
        ["settings"] = run.Settings is null ? BsonNull.Value : new BsonDocument(run.Settings.ToDictionary(s => s.Key, s => (object)s.Value)),
        ["modes"] = new BsonArray((run.Modes ?? []).Select(m => new BsonDocument
        {
            ["name"] = m.Name, ["repeat"] = m.Repeat, ["toolsets"] = new BsonArray(m.Toolsets),
            ["role"] = Nullable(m.Role), ["llm"] = Nullable(m.Llm),
        })),
        ["total"] = run.Total is null ? BsonNull.Value : new BsonDocument
        {
            ["cost"] = Nullable(run.Total.Cost), ["currency"] = run.Total.Currency, ["tokens"] = run.Total.Tokens, ["requests"] = run.Total.Requests,
        },
        ["log"] = run.Log is null ? BsonNull.Value : new BsonArray(run.Log),
        ["store_error"] = Nullable(run.StoreError),
        // F18 step 2a: what the run ran. Absent in a run stored before it; read as null.
        ["eval"] = Nullable(run.Eval),
        ["eval_version"] = Nullable(run.EvalVersion),
        ["commit"] = Nullable(run.Commit),
        ["graders"] = run.Graders is null ? BsonNull.Value : new BsonArray(run.Graders.Select(g => new BsonDocument
        {
            ["name"] = g.Name, ["version"] = g.Version, ["kind"] = g.Kind.ToString(),
        })),
        ["models"] = run.Models is null ? BsonNull.Value : new BsonArray(run.Models.Select(m => new BsonDocument
        {
            ["mode"] = m.Mode, ["level"] = m.Level.ToString(), ["role"] = Nullable(m.Role), ["model"] = Nullable(m.Model),
        })),
    };

    /// <summary>A run as stored; a mode recorded before CL14 keeps "personality", read as its role (never rewritten).</summary>
    internal static Run RunOf(BsonDocument d) => new(
        d["_id"].AsString,
        d["check"].AsString,
        d["repeat"].AsInt32,
        Enum.Parse<BenchmarkRunStatus>(d["status"].AsString, ignoreCase: true),
        DateOf(d["started_at"])!.Value,
        DateOf(d["ended_at"]),
        d["exit_code"].IsBsonNull ? null : d["exit_code"].AsInt32,
        d["settings"].IsBsonNull ? null : d["settings"].AsBsonDocument.ToDictionary(e => e.Name, e => e.Value.AsString),
        d["modes"].AsBsonArray.Select(m => m.AsBsonDocument).Select(m => new Mode(m["name"].AsString, m["repeat"].AsInt32,
            m["toolsets"].AsBsonArray.Select(t => t.AsString).ToArray(),
            StringOf(m.GetValue("role", null) ?? m.GetValue("personality", BsonNull.Value)), StringOf(m["llm"]))).ToArray(),
        d["total"].IsBsonNull ? null : new Total(DecimalOf(d["total"]["cost"]), d["total"]["currency"].AsString,
            d["total"]["tokens"].ToInt64(), d["total"]["requests"].AsInt32),
        d["log"].IsBsonNull ? null : d["log"].AsBsonArray.Select(l => l.AsString).ToArray(),
        StringOf(d["store_error"]),
        StringOf(d.GetValue("eval", BsonNull.Value)),
        d.GetValue("eval_version", BsonNull.Value).IsBsonNull ? null : d["eval_version"].AsInt32,
        StringOf(d.GetValue("commit", BsonNull.Value)),
        d.GetValue("graders", BsonNull.Value).IsBsonNull ? null : d["graders"].AsBsonArray.Select(g => g.AsBsonDocument)
            .Select(g => new Grader(g["name"].AsString, g["version"].AsInt32, Enum.Parse<BenchmarkGraderKind>(g["kind"].AsString, ignoreCase: true))).ToArray(),
        d.GetValue("models", BsonNull.Value).IsBsonNull ? null : d["models"].AsBsonArray.Select(m => m.AsBsonDocument)
            .Select(m => new ModelUse(m["mode"].AsString, Enum.Parse<BenchmarkModelLevel>(m["level"].AsString, ignoreCase: true), StringOf(m.GetValue("role", BsonNull.Value)), StringOf(m.GetValue("model", BsonNull.Value)))).ToArray());

    private static BsonDocument ToBson(string run, Conversation c) => new()
    {
        ["_id"] = ConversationId(run, c.Key),
        ["run"] = run, ["mode"] = c.Key.Mode, ["repeat"] = c.Key.Repeat, ["task"] = c.Key.Task,
        ["order"] = c.Order, ["role"] = Nullable(c.Role), ["scores"] = Bson(c.Scores), ["errors"] = c.Errors, ["seconds"] = c.Seconds,
        ["grades"] = c.Grades is null ? BsonNull.Value : new BsonArray(c.Grades.Select(g => new BsonDocument
        {
            ["grader"] = g.Grader, ["version"] = g.Version, ["score"] = g.Score, ["outcome"] = g.Outcome.ToString(), ["reason"] = g.Reason,
        })),
        ["outcome"] = Nullable(c.Outcome?.ToString()),
    };

    private static Conversation ConversationOf(BsonDocument d) => new(
        new ConversationKey(d["mode"].AsString, d["repeat"].AsInt32, d["task"].AsString),
        d["order"].AsInt32, StringOf(d["role"]), Json(d["scores"]), d["errors"].AsInt32, d["seconds"].ToDouble(),
        d.GetValue("grades", BsonNull.Value).IsBsonNull ? null : d["grades"].AsBsonArray.Select(g => g.AsBsonDocument)
            .Select(g => new Grade(g["grader"].AsString, g["version"].AsInt32, g["score"].ToDouble(), Enum.Parse<BenchmarkOutcome>(g["outcome"].AsString, ignoreCase: true), g["reason"].AsString)).ToArray(),
        d.GetValue("outcome", BsonNull.Value).IsBsonNull ? null : Enum.Parse<BenchmarkOutcome>(d["outcome"].AsString, ignoreCase: true));

    private static BsonDocument ToBson(string run, Turn t, IReadOnlyList<StreamEvent> events) => new()
    {
        ["_id"] = TurnId(run, t.Key),
        ["run"] = run, ["mode"] = t.Key.Mode, ["repeat"] = t.Key.Repeat, ["task"] = t.Key.Task, ["turn"] = t.Key.Turn,
        ["session"] = t.Session, ["question"] = t.Question, ["answer"] = Nullable(t.Answer), ["role"] = Nullable(t.Role),
        ["continued"] = Nullable(t.Continued), ["puls_calls"] = t.PulsCalls, ["specialist_calls"] = t.SpecialistCalls,
        ["hosts"] = new BsonArray(t.Hosts), ["seconds"] = t.Seconds, ["error"] = Nullable(t.Error),
        ["tools"] = t.Tools is { } tools
            ? new BsonDocument
            {
                ["calls"] = tools.Calls, ["failed"] = tools.Failed,
                ["reasons"] = new BsonArray(tools.Reasons.Select(r => new BsonDocument { ["tool"] = r.Tool, ["reason"] = r.Reason, ["count"] = r.Count })),
            }
            : BsonNull.Value,
        ["events"] = new BsonArray(events.Select(e => new BsonDocument { ["type"] = e.Type, ["data"] = Bson(e.Data), ["at"] = Date(e.At) })),
    };

    private static Turn TurnOf(BsonDocument d) => new(
        new TurnKey(d["mode"].AsString, d["repeat"].AsInt32, d["task"].AsString, d["turn"].AsInt32),
        d["session"].AsString, d["question"].AsString, StringOf(d["answer"]), StringOf(d["role"]), StringOf(d["continued"]),
        d["puls_calls"].AsInt32, d["specialist_calls"].AsInt32, d["hosts"].AsBsonArray.Select(h => h.AsString).ToArray(),
        d["seconds"].ToDouble(), StringOf(d["error"]), ToolsOf(d.GetValue("tools", BsonNull.Value)));

    /// <summary>A turn's kept tool counts; a turn recorded before they were kept has none (null, never 0 calls).</summary>
    private static BenchmarkTools.Summary? ToolsOf(BsonValue v) => v is BsonDocument t
        ? new BenchmarkTools.Summary(t["calls"].AsInt32, t["failed"].AsInt32,
            t["reasons"].AsBsonArray.Select(r => new BenchmarkTools.Failure(r["tool"].AsString, r["reason"].AsString, r["count"].AsInt32)).ToArray())
        : null;

    private static StreamEvent EventOf(BsonDocument e) => new(e["type"].AsString, Json(e["data"]), DateOf(e["at"]));

    private static BsonDocument ToBson(Request r) => new()
    {
        ["run"] = r.Run,
        ["turn"] = r.Turn is null ? BsonNull.Value : new BsonDocument { ["mode"] = r.Turn.Mode, ["repeat"] = r.Turn.Repeat, ["task"] = r.Turn.Task, ["turn"] = r.Turn.Turn },
        ["at"] = Date(r.At), ["purpose"] = r.Purpose, ["session"] = Nullable(r.Session), ["parent"] = Nullable(r.Parent),
        ["model"] = Nullable(r.Model), ["input_tokens"] = Nullable(r.InputTokens), ["output_tokens"] = Nullable(r.OutputTokens),
        ["cache_read_tokens"] = Nullable(r.CacheReadTokens), ["cache_write_tokens"] = Nullable(r.CacheWriteTokens),
        ["cost"] = Nullable(r.Cost), ["duration_ms"] = r.DurationMs,
    };

    private static Request RequestOf(BsonDocument d) => new(
        d["run"].AsString,
        d["turn"].IsBsonNull ? null : new TurnKey(d["turn"]["mode"].AsString, d["turn"]["repeat"].AsInt32, d["turn"]["task"].AsString, d["turn"]["turn"].AsInt32),
        DateOf(d["at"])!.Value, d["purpose"].AsString, StringOf(d["session"]), StringOf(d["parent"]), StringOf(d["model"]),
        LongOf(d["input_tokens"]), LongOf(d["output_tokens"]), LongOf(d["cache_read_tokens"]), LongOf(d["cache_write_tokens"]),
        DecimalOf(d["cost"]), d["duration_ms"].ToInt64());

    // The values.

    private static string Status(BenchmarkRunStatus status) => status.ToString().ToLowerInvariant();

    private static BsonValue Date(DateTimeOffset? at) => at is { } value ? new BsonDateTime(value.UtcDateTime) : BsonNull.Value;

    private static DateTimeOffset? DateOf(BsonValue value) =>
        value.IsBsonNull ? null : new DateTimeOffset(value.ToUniversalTime(), TimeSpan.Zero);

    private static BsonValue Nullable(string? value) => value is null ? BsonNull.Value : new BsonString(value);
    private static BsonValue Nullable(int? value) => value is null ? BsonNull.Value : new BsonInt32(value.Value);
    private static BsonValue Nullable(long? value) => value is null ? BsonNull.Value : new BsonInt64(value.Value);
    private static BsonValue Nullable(decimal? value) => value is null ? BsonNull.Value : new BsonDecimal128(value.Value);

    private static string? StringOf(BsonValue value) => value.IsBsonNull ? null : value.AsString;
    private static long? LongOf(BsonValue value) => value.IsBsonNull ? null : value.ToInt64();
    private static decimal? DecimalOf(BsonValue value) => value.IsBsonNull ? null : value.ToDecimal();

    /// <summary>JSON as BSON, any JSON value (an object, a list, a number), wrapped and unwrapped since BSON's root is a document.</summary>
    private static BsonValue Bson(JsonElement json) => BsonDocument.Parse($$"""{"v":{{json.GetRawText()}}}""")["v"];

    private static readonly JsonWriterSettings RelaxedJson = new() { OutputMode = JsonOutputMode.RelaxedExtendedJson };

    private static JsonElement Json(BsonValue bson)
    {
        using var document = JsonDocument.Parse(new BsonDocument("v", bson).ToJson(RelaxedJson));
        return document.RootElement.GetProperty("v").Clone();
    }
}
