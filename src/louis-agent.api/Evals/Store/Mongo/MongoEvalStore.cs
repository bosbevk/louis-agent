using LouisAgent.Api.Evals.Store.Conversations;
using LouisAgent.Api.Evals.Store.Mongo.Documents;
using LouisAgent.Api.Evals.Store.Notes;
using LouisAgent.Api.Evals.Store.Requests;
using LouisAgent.Api.Evals.Store.Runs;
using LouisAgent.Api.Evals.Store.Turns;
using MongoDB.Bson;
using MongoDB.Driver;
using LouisAgent.Evals.Protocol.Parts.Conversations;
using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Evals.Store.Mongo;

/// <summary>
/// Keeps eval runs in MongoDB, one collection per kind of record: runs, conversations, turns, the ledger's requests
/// and the notes. Each record has its own store; this class adds the notes and deleting a run with everything under
/// it.
/// </summary>
/// <remarks>
/// Facade over one Repository per record. The runs and the conversations are typed collections of a document record
/// (<see cref="RunDocument"/>, <see cref="ConversationDocument"/>, ADR 0048); the turns and the requests still have
/// their hand-written Data Mapper (<see cref="TurnDocuments"/>, <see cref="RequestDocuments"/>) until their own
/// commits. The callers keep one store, and each record store stays one collection with its own contract test. Every
/// store makes its index the first time it is needed, so starting the API never waits on Mongo.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0022-benchmarks-recorded-from-the-protocol.md">ADR 0022: benchmark results in MongoDB</seealso>
public sealed class MongoEvalStore : IEvalStore
{
    /// <summary>The runs themselves.</summary>
    private readonly MongoRunStore _runs;

    /// <summary>The conversations.</summary>
    private readonly MongoConversationStore _conversations;

    /// <summary>The turns, with their stream events inside.</summary>
    private readonly MongoTurnStore _turns;

    /// <summary>The ledger's requests.</summary>
    private readonly MongoRequestStore _requests;

    /// <summary>The notes, which live in a collection of their own.</summary>
    private readonly MongoRunNotes _notes;

    /// <summary>Makes the notes' index the first time anything needs it, and tries again if that failed.</summary>
    private readonly OnceItWorks _notesIndex;

    /// <summary>
    /// Connects to the database on the server at the address, with short timeouts. Nothing is created until the first
    /// call.
    /// </summary>
    public MongoEvalStore(string url, string database)
    {
        Database = MongoEvalDatabase.Open(url, database);
        _runs = new MongoRunStore(Database);
        _conversations = new MongoConversationStore(Database);
        _turns = new MongoTurnStore(Database);
        _requests = new MongoRequestStore(Database);

        // A note may only go on a run that exists, so the notes ask the runs.
        _notes = new MongoRunNotes(Database.GetCollection<BsonDocument>("run_notes"),
            async (id, cancellationToken) => await _runs.GetRunAsync(id, cancellationToken) is not null);
        _notesIndex = new OnceItWorks(_notes.CreateIndexAsync);
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

    /// <summary>Hands the turn and its events to the turn store.</summary>
    public Task SaveTurnAsync(string run, Turn turn, IReadOnlyList<StreamEvent> events,
        CancellationToken cancellationToken = default) => _turns.SaveTurnAsync(run, turn, events, cancellationToken);

    /// <summary>Asks the turn store for a conversation's turns.</summary>
    public Task<IReadOnlyList<Turn>> TurnsAsync(string run, ConversationKey conversation,
        CancellationToken cancellationToken = default) => _turns.TurnsAsync(run, conversation, cancellationToken);

    /// <summary>Asks the turn store for a turn's stream events.</summary>
    public Task<IReadOnlyList<StreamEvent>> TurnEventsAsync(string run, TurnKey turn,
        CancellationToken cancellationToken = default) => _turns.TurnEventsAsync(run, turn, cancellationToken);

    /// <summary>Deletes only the run's turns.</summary>
    public Task<bool> DeleteTurnsAsync(string run, CancellationToken cancellationToken = default) =>
        _turns.DeleteTurnsAsync(run, cancellationToken);

    /// <summary>Hands the ledger's requests to the request store.</summary>
    public Task AddRequestsAsync(string run, IReadOnlyList<Request> requests,
        CancellationToken cancellationToken = default) => _requests.AddRequestsAsync(run, requests, cancellationToken);

    /// <summary>Asks the request store for the run's requests.</summary>
    public Task<IReadOnlyList<Request>> RequestsAsync(string run, CancellationToken cancellationToken = default) =>
        _requests.RequestsAsync(run, cancellationToken);

    /// <summary>Deletes only the run's requests.</summary>
    public Task<bool> DeleteRequestsAsync(string run, CancellationToken cancellationToken = default) =>
        _requests.DeleteRequestsAsync(run, cancellationToken);

    /// <summary>Adds a person's note to a run, or to one of its conversations.</summary>
    public async Task<NoteResult> AddNoteAsync(string run, string? text, DateTimeOffset at,
        ConversationKey? conversation = null, CancellationToken cancellationToken = default)
    {
        await _notesIndex.RunAsync();
        return await _notes.AddAsync(run, text, at, conversation, cancellationToken);
    }

    /// <summary>The run's notes, in the order they were written.</summary>
    public async Task<IReadOnlyList<RunNote>> NotesAsync(string run, CancellationToken cancellationToken = default)
    {
        await _notesIndex.RunAsync();
        return await _notes.OfRunAsync(run, cancellationToken);
    }

    /// <summary>Deletes the run and everything under it, notes included, and says whether there was anything.</summary>
    public async Task<bool> DeleteRunAsync(string id, CancellationToken cancellationToken = default)
    {
        // Each store deletes its own part; anything found counts as a run having been there.
        bool had = await _runs.DeleteAsync(id, cancellationToken);
        had |= await _conversations.DeleteConversationsAsync(id, cancellationToken);
        had |= await _turns.DeleteTurnsAsync(id, cancellationToken);
        had |= await _requests.DeleteRequestsAsync(id, cancellationToken);

        await _notesIndex.RunAsync();
        long notes = await _notes.DeleteRunAsync(id, cancellationToken);
        return had || notes > 0;
    }
}
