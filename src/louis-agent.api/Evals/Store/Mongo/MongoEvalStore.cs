using LouisAgent.Api.Evals.Runs.Kinds;
using LouisAgent.Api.Evals.Store.Mongo.Documents;
using LouisAgent.Api.Evals.Store.Notes;
using MongoDB.Bson;
using MongoDB.Driver;
using static LouisAgent.Api.Evals.Runs.Protocol.EvalProtocol;
using static LouisAgent.Api.Evals.Store.EvalDocuments;
using static LouisAgent.Api.Evals.Store.Mongo.BsonValues;

namespace LouisAgent.Api.Evals.Store.Mongo;

/// <summary>
/// Keeps eval runs in MongoDB, one collection per kind of record: runs, conversations, turns, the ledger's requests
/// and the notes. A server that doesn't answer fails a call within five seconds, so the recorder gives up quickly and
/// the run carries on.
/// </summary>
/// <remarks>
/// Repository, with a Data Mapper per document (<see cref="RunDocuments"/>, <see cref="ConversationDocuments"/>,
/// <see cref="TurnDocuments"/>, <see cref="RequestDocuments"/>), written by hand so what <c>mongosh</c> shows is what
/// the portal reads. This class keeps only the collections and the queries; the indexes are made the first time
/// anything needs them, so starting the API never waits on Mongo.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0022-benchmarks-recorded-from-the-protocol.md">ADR 0022: benchmark results in MongoDB</seealso>
public sealed class MongoEvalStore : IEvalStore
{
    /// <summary>How long a call waits for the server before giving up.</summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    /// <summary>Replace the document if there is one, otherwise insert it.</summary>
    private static readonly ReplaceOptions Upsert = new() { IsUpsert = true };

    /// <summary>The runs.</summary>
    private readonly IMongoCollection<BsonDocument> _runs;

    /// <summary>The conversations.</summary>
    private readonly IMongoCollection<BsonDocument> _conversations;

    /// <summary>The turns, with their stream events inside.</summary>
    private readonly IMongoCollection<BsonDocument> _turns;

    /// <summary>The ledger's requests.</summary>
    private readonly IMongoCollection<BsonDocument> _requests;

    /// <summary>The notes, which live in a collection of their own.</summary>
    private readonly MongoRunNotes _notes;

    /// <summary>Makes the indexes the first time anything needs them, and tries again if that failed.</summary>
    private readonly OnceItWorks _indexes;

    /// <summary>
    /// Connects to the database on the server at the address, with short timeouts. Nothing is created until the first
    /// call.
    /// </summary>
    public MongoEvalStore(string url, string database)
    {
        var settings = MongoClientSettings.FromConnectionString(url);
        settings.ServerSelectionTimeout = Timeout;
        settings.ConnectTimeout = Timeout;

        var db = new MongoClient(settings).GetDatabase(database);
        _runs = db.GetCollection<BsonDocument>("runs");
        _conversations = db.GetCollection<BsonDocument>("conversations");
        _turns = db.GetCollection<BsonDocument>("turns");
        _requests = db.GetCollection<BsonDocument>("requests");
        // A note may only go on a run that exists, so the notes ask the runs.
        _notes = new MongoRunNotes(db.GetCollection<BsonDocument>("run_notes"),
            (id, cancellationToken) => _runs.Find(ById(id)).AnyAsync(cancellationToken));
        _indexes = new OnceItWorks(CreateIndexesAsync);
    }

    /// <summary>The database itself, so a test can drop its own.</summary>
    internal IMongoDatabase Database => _runs.Database;

    /// <summary>Saves the run, replacing the one under its id.</summary>
    public async Task SaveRunAsync(Run run, CancellationToken cancellationToken = default)
    {
        await Ready();
        await _runs.ReplaceOneAsync(ById(run.Id), RunDocuments.ToBson(run), Upsert, cancellationToken);
    }

    /// <summary>Finds a run by its id.</summary>
    public async Task<Run?> GetRunAsync(string id, CancellationToken cancellationToken = default)
    {
        await Ready();
        var found = await _runs.Find(ById(id)).FirstOrDefaultAsync(cancellationToken);
        return found is null ? null : RunDocuments.RunOf(found);
    }

    /// <summary>The newest runs first, up to the limit, and one eval's only if asked.</summary>
    public async Task<IReadOnlyList<Run>> ListRunsAsync(string? check = null, int limit = 50,
        CancellationToken cancellationToken = default)
    {
        await Ready();
        var filter = check is null
            ? Builders<BsonDocument>.Filter.Empty
            : Builders<BsonDocument>.Filter.Eq("check", check);
        var found = await _runs.Find(filter).Sort(Builders<BsonDocument>.Sort.Descending("started_at")).Limit(limit)
            .ToListAsync(cancellationToken);
        return found.Select(RunDocuments.RunOf).ToArray();
    }

    /// <summary>Saves a conversation, replacing the one under its key.</summary>
    public async Task SaveConversationAsync(string run, Conversation conversation,
        CancellationToken cancellationToken = default)
    {
        await Ready();
        await _conversations.ReplaceOneAsync(ById(ConversationDocuments.Id(run, conversation.Key)),
            ConversationDocuments.ToBson(run, conversation), Upsert, cancellationToken);
    }

    /// <summary>The run's conversations in the order they started.</summary>
    public async Task<IReadOnlyList<Conversation>> ConversationsAsync(string run,
        CancellationToken cancellationToken = default)
    {
        await Ready();
        var found = await _conversations.Find(OfRun(run)).Sort(Builders<BsonDocument>.Sort.Ascending("order"))
            .ToListAsync(cancellationToken);
        return found.Select(ConversationDocuments.ConversationOf).ToArray();
    }

    /// <summary>Saves a turn with its stream events, replacing the ones under its key.</summary>
    public async Task SaveTurnAsync(string run, Turn turn, IReadOnlyList<StreamEvent> events,
        CancellationToken cancellationToken = default)
    {
        await Ready();
        await _turns.ReplaceOneAsync(ById(TurnDocuments.Id(run, turn.Key)), TurnDocuments.ToBson(run, turn, events),
            Upsert, cancellationToken);
    }

    /// <summary>A conversation's turns in order, leaving the heavy stream events out.</summary>
    public async Task<IReadOnlyList<Turn>> TurnsAsync(string run, ConversationKey conversation,
        CancellationToken cancellationToken = default)
    {
        await Ready();
        var filter = Builders<BsonDocument>.Filter.And(OfRun(run),
            Builders<BsonDocument>.Filter.Eq("mode", conversation.Mode),
            Builders<BsonDocument>.Filter.Eq("repeat", conversation.Repeat),
            Builders<BsonDocument>.Filter.Eq("task", conversation.Task));
        var found = await _turns.Find(filter).Project<BsonDocument>(Builders<BsonDocument>.Projection.Exclude("events"))
            .Sort(Builders<BsonDocument>.Sort.Ascending("turn")).ToListAsync(cancellationToken);
        return found.Select(TurnDocuments.TurnOf).ToArray();
    }

    /// <summary>A turn's stream events in the order they came; none for a turn it doesn't know.</summary>
    public async Task<IReadOnlyList<StreamEvent>> TurnEventsAsync(string run, TurnKey turn,
        CancellationToken cancellationToken = default)
    {
        await Ready();
        var found = await _turns.Find(ById(TurnDocuments.Id(run, turn)))
            .Project<BsonDocument>(Builders<BsonDocument>.Projection.Include("events"))
            .FirstOrDefaultAsync(cancellationToken);
        return found?["events"].AsBsonArray.Select(e => TurnDocuments.EventOf(e.AsBsonDocument)).ToArray() ?? [];
    }

    /// <summary>Adds the ledger's requests, tagged with their run.</summary>
    public async Task AddRequestsAsync(string run, IReadOnlyList<Request> requests,
        CancellationToken cancellationToken = default)
    {
        // Mongo refuses an empty insert, and there's nothing to add anyway.
        if (requests.Count == 0) return;

        await Ready();
        await _requests.InsertManyAsync(requests.Select(r => RequestDocuments.ToBson(r with { Run = run })),
            cancellationToken: cancellationToken);
    }

    /// <summary>The run's requests, by time.</summary>
    public async Task<IReadOnlyList<Request>> RequestsAsync(string run, CancellationToken cancellationToken = default)
    {
        await Ready();
        var found = await _requests.Find(OfRun(run)).Sort(Builders<BsonDocument>.Sort.Ascending("at"))
            .ToListAsync(cancellationToken);
        return found.Select(RequestDocuments.RequestOf).ToArray();
    }

    /// <summary>Marks every run still running as failed and ended now, and says how many there were.</summary>
    public async Task<int> CloseOrphansAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        await Ready();
        var result = await _runs.UpdateManyAsync(
            Builders<BsonDocument>.Filter.Eq("status", RunDocuments.Status(EvalRunStatus.Running)),
            Builders<BsonDocument>.Update.Set("status", RunDocuments.Status(EvalRunStatus.Failed))
                .Set("ended_at", Date(now)),
            cancellationToken: cancellationToken);
        return (int)result.ModifiedCount;
    }

    /// <summary>Adds a person's note to a run, or to one of its conversations.</summary>
    public async Task<NoteResult> AddNoteAsync(string run, string? text, DateTimeOffset at,
        ConversationKey? conversation = null, CancellationToken cancellationToken = default)
    {
        await Ready();
        return await _notes.AddAsync(run, text, at, conversation, cancellationToken);
    }

    /// <summary>The run's notes, in the order they were written.</summary>
    public async Task<IReadOnlyList<RunNote>> NotesAsync(string run, CancellationToken cancellationToken = default)
    {
        await Ready();
        return await _notes.OfRunAsync(run, cancellationToken);
    }

    /// <summary>Deletes the run and everything under it, notes included, and says whether there was anything.</summary>
    public async Task<bool> DeleteRunAsync(string id, CancellationToken cancellationToken = default)
    {
        await Ready();
        long deleted = (await _runs.DeleteOneAsync(ById(id), cancellationToken)).DeletedCount;
        deleted += (await _conversations.DeleteManyAsync(OfRun(id), cancellationToken)).DeletedCount;
        deleted += (await _turns.DeleteManyAsync(OfRun(id), cancellationToken)).DeletedCount;
        deleted += (await _requests.DeleteManyAsync(OfRun(id), cancellationToken)).DeletedCount;
        deleted += await _notes.DeleteRunAsync(id, cancellationToken);
        return deleted > 0;
    }

    /// <summary>Makes the indexes the reads rely on.</summary>
    private async Task CreateIndexesAsync()
    {
        await _runs.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("check").Descending("started_at")));
        await _conversations.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("run").Ascending("order")));
        await _turns.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("run").Ascending("mode").Ascending("repeat").Ascending("task")
                .Ascending("turn")));
        await _requests.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("run").Ascending("at")));
        await _notes.CreateIndexAsync();
    }

    /// <summary>Waits until the indexes exist, making them if this is the first call.</summary>
    private Task Ready() => _indexes.RunAsync();

    /// <summary>Finds the document with this id.</summary>
    private static FilterDefinition<BsonDocument> ById(BsonValue id) => Builders<BsonDocument>.Filter.Eq("_id", id);

    /// <summary>Finds everything that belongs to this run.</summary>
    private static FilterDefinition<BsonDocument> OfRun(string run) => Builders<BsonDocument>.Filter.Eq("run", run);
}
