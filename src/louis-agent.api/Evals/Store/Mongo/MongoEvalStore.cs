using LouisAgent.Api.Evals.Store.Conversations;
using LouisAgent.Api.Evals.Store.Mongo.Documents;
using LouisAgent.Api.Evals.Store.Notes;
using LouisAgent.Api.Evals.Store.Runs;
using MongoDB.Bson;
using MongoDB.Driver;
using static LouisAgent.Api.Evals.Runs.Protocol.EvalProtocol;
using static LouisAgent.Api.Evals.Store.EvalDocuments;
using static LouisAgent.Api.Evals.Store.Mongo.MongoEvalDatabase;

namespace LouisAgent.Api.Evals.Store.Mongo;

/// <summary>
/// Keeps eval runs in MongoDB, one collection per kind of record: runs, conversations, turns, the ledger's requests
/// and the notes. Each record has its own store; this class adds the notes and deleting a run with everything under
/// it.
/// </summary>
/// <remarks>
/// Facade over one Repository per record, each with its hand-written Data Mapper (<see cref="RunDocuments"/>,
/// <see cref="ConversationDocuments"/>, <see cref="TurnDocuments"/>, <see cref="RequestDocuments"/>), so what
/// <c>mongosh</c> shows is what the portal reads. The indexes are made the first time anything needs them, so
/// starting the API never waits on Mongo.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0022-benchmarks-recorded-from-the-protocol.md">ADR 0022: benchmark results in MongoDB</seealso>
public sealed class MongoEvalStore : IEvalStore
{
    /// <summary>The runs themselves.</summary>
    private readonly MongoRunStore _runs;

    /// <summary>The conversations.</summary>
    private readonly MongoConversationStore _conversations;

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
        Database = MongoEvalDatabase.Open(url, database);
        _runs = new MongoRunStore(Database);
        _conversations = new MongoConversationStore(Database);
        _turns = Database.GetCollection<BsonDocument>("turns");
        _requests = Database.GetCollection<BsonDocument>("requests");
        // A note may only go on a run that exists, so the notes ask the runs.
        _notes = new MongoRunNotes(Database.GetCollection<BsonDocument>("run_notes"),
            async (id, cancellationToken) => await _runs.GetRunAsync(id, cancellationToken) is not null);
        _indexes = new OnceItWorks(CreateIndexesAsync);
    }

    /// <summary>The database itself, so a test can drop its own.</summary>
    internal IMongoDatabase Database { get; }

    /// <summary>Hands the run to the run store.</summary>
    public Task SaveRunAsync(Run run, CancellationToken cancellationToken = default) =>
        _runs.SaveRunAsync(run, cancellationToken);

    /// <summary>Asks the run store for the run.</summary>
    public Task<Run?> GetRunAsync(string id, CancellationToken cancellationToken = default) =>
        _runs.GetRunAsync(id, cancellationToken);

    /// <summary>Asks the run store for the newest runs.</summary>
    public Task<IReadOnlyList<Run>> ListRunsAsync(string? check = null, int limit = 50,
        CancellationToken cancellationToken = default) => _runs.ListRunsAsync(check, limit, cancellationToken);

    /// <summary>Has the run store close the runs a restart cut off.</summary>
    public Task<int> CloseOrphansAsync(DateTimeOffset now, CancellationToken cancellationToken = default) =>
        _runs.CloseOrphansAsync(now, cancellationToken);

    /// <summary>Deletes only the run's own document.</summary>
    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default) =>
        _runs.DeleteAsync(id, cancellationToken);

    /// <summary>Hands the conversation to the conversation store.</summary>
    public Task SaveConversationAsync(string run, Conversation conversation,
        CancellationToken cancellationToken = default) =>
        _conversations.SaveConversationAsync(run, conversation, cancellationToken);

    /// <summary>Asks the conversation store for the run's conversations.</summary>
    public Task<IReadOnlyList<Conversation>> ConversationsAsync(string run,
        CancellationToken cancellationToken = default) => _conversations.ConversationsAsync(run, cancellationToken);

    /// <summary>Deletes only the run's conversations.</summary>
    public Task<bool> DeleteConversationsAsync(string run, CancellationToken cancellationToken = default) =>
        _conversations.DeleteConversationsAsync(run, cancellationToken);

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
        bool had = await _runs.DeleteAsync(id, cancellationToken);
        had |= await _conversations.DeleteConversationsAsync(id, cancellationToken);
        long deleted = (await _turns.DeleteManyAsync(OfRun(id), cancellationToken)).DeletedCount;
        deleted += (await _requests.DeleteManyAsync(OfRun(id), cancellationToken)).DeletedCount;
        deleted += await _notes.DeleteRunAsync(id, cancellationToken);
        return had || deleted > 0;
    }

    /// <summary>Makes the indexes the reads of the records still kept here rely on.</summary>
    private async Task CreateIndexesAsync()
    {
        await _turns.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("run").Ascending("mode").Ascending("repeat").Ascending("task")
                .Ascending("turn")));
        await _requests.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("run").Ascending("at")));
        await _notes.CreateIndexAsync();
    }

    /// <summary>Waits until the indexes exist, making them if this is the first call.</summary>
    private Task Ready() => _indexes.RunAsync();
}
