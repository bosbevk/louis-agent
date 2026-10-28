using LouisAgent.Api.Evals.Store.Mongo;
using LouisAgent.Api.Evals.Store.Mongo.Documents;
using MongoDB.Bson;
using MongoDB.Driver;
using static LouisAgent.Api.Evals.Store.EvalDocuments;
using static LouisAgent.Api.Evals.Runs.Protocol.EvalProtocol;
using static LouisAgent.Api.Evals.Store.Mongo.MongoEvalDatabase;

namespace LouisAgent.Api.Evals.Store.Turns;

/// <summary>
/// Keeps the turns in MongoDB's <c>turns</c> collection, one document per turn with its stream events inside. Reading
/// a conversation's turns leaves the events out, which are heavy. Its index (by conversation, then turn) is made the
/// first time anything needs it, so starting the API never waits on Mongo.
/// </summary>
/// <remarks>
/// Repository, with a Data Mapper (<see cref="TurnDocuments"/>) written by hand, so what <c>mongosh</c> shows is what
/// the portal reads and the events stay the JSON they came as. This class keeps only the collection and its queries.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0022-benchmarks-recorded-from-the-protocol.md">ADR 0022: benchmark results in MongoDB</seealso>
public sealed class MongoTurnStore : ITurnStore
{
    /// <summary>The turns, with their stream events inside.</summary>
    private readonly IMongoCollection<BsonDocument> _turns;

    /// <summary>Makes the index the first time anything needs it, and tries again if that failed.</summary>
    private readonly OnceItWorks _index;

    /// <summary>Uses the database's turns collection. Nothing is created until the first call.</summary>
    public MongoTurnStore(IMongoDatabase database)
    {
        _turns = database.GetCollection<BsonDocument>("turns");
        _index = new OnceItWorks(() => _turns.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("run").Ascending("mode").Ascending("repeat").Ascending("task")
                .Ascending("turn"))));
    }

    /// <summary>Saves a turn with its stream events, replacing the ones under its key.</summary>
    public async Task SaveTurnAsync(string run, Turn turn, IReadOnlyList<StreamEvent> events,
        CancellationToken cancellationToken = default)
    {
        await _index.RunAsync();
        await _turns.ReplaceOneAsync(ById(TurnDocuments.Id(run, turn.Key)), TurnDocuments.ToBson(run, turn, events),
            Upsert, cancellationToken);
    }

    /// <summary>A conversation's turns in order, leaving the heavy stream events out.</summary>
    public async Task<IReadOnlyList<Turn>> TurnsAsync(string run, ConversationKey conversation,
        CancellationToken cancellationToken = default)
    {
        await _index.RunAsync();
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
        await _index.RunAsync();
        var found = await _turns.Find(ById(TurnDocuments.Id(run, turn)))
            .Project<BsonDocument>(Builders<BsonDocument>.Projection.Include("events"))
            .FirstOrDefaultAsync(cancellationToken);
        return found?["events"].AsBsonArray.Select(e => TurnDocuments.EventOf(e.AsBsonDocument)).ToArray() ?? [];
    }

    /// <summary>Deletes the run's turns, events and all, and says whether there were any.</summary>
    public async Task<bool> DeleteTurnsAsync(string run, CancellationToken cancellationToken = default)
    {
        await _index.RunAsync();
        return (await _turns.DeleteManyAsync(OfRun(run), cancellationToken)).DeletedCount > 0;
    }
}
