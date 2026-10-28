using LouisAgent.Api.Evals.Store.Mongo.Documents;
using LouisAgent.Evals.Protocol.Parts.Conversations;
using MongoDB.Driver;
using static LouisAgent.Api.Evals.Store.EvalDocuments;
using static LouisAgent.Api.Evals.Store.Mongo.MongoEvalDatabase;

namespace LouisAgent.Api.Evals.Store.Turns;

/// <summary>
/// Keeps the turns in MongoDB's <c>turns</c> collection, one document per turn with its stream events inside. Reading
/// a conversation's turns leaves the events out, which are heavy. Its index (by conversation, then turn) is made the
/// first time anything needs it, so starting the API never waits on Mongo.
/// </summary>
/// <remarks>
/// Repository over a typed collection (ADR 0048): the driver maps each turn's <see cref="TurnDocument"/>, and every
/// filter, sort, projection and index names its field through the record, so a renamed field fails the build. This
/// class keeps only the collection and its queries.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0022-benchmarks-recorded-from-the-protocol.md">ADR 0022: benchmark results in MongoDB</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
public sealed class MongoTurnStore : ITurnStore
{
    /// <summary>The turns, with their stream events inside.</summary>
    private readonly IMongoCollection<TurnDocument> _turns;

    /// <summary>Makes the index the first time anything needs it, and tries again if that failed.</summary>
    private readonly OnceItWorks _index;

    /// <summary>Uses the database's turns collection. Nothing is created until the first call.</summary>
    public MongoTurnStore(IMongoDatabase database)
    {
        // The driver must know how enums and times are stored before it maps its first document.
        RegisterMappings();
        _turns = database.GetCollection<TurnDocument>("turns");
        _index = new OnceItWorks(() => _turns.Indexes.CreateOneAsync(new CreateIndexModel<TurnDocument>(
            Builders<TurnDocument>.IndexKeys.Ascending(d => d.Run).Ascending(d => d.Mode).Ascending(d => d.Repeat)
                .Ascending(d => d.Task).Ascending(d => d.Turn))));
    }

    /// <summary>Saves a turn with its stream events, replacing the ones under its key.</summary>
    public async Task SaveTurnAsync(string run, Turn turn, IReadOnlyList<StreamEvent> events,
        CancellationToken cancellationToken = default)
    {
        await _index.RunAsync();
        var document = TurnDocument.Of(run, turn, events);
        await _turns.ReplaceOneAsync(ById(document.Id), document, Upsert, cancellationToken);
    }

    /// <summary>A conversation's turns in order, leaving the heavy stream events out.</summary>
    public async Task<IReadOnlyList<Turn>> TurnsAsync(string run, ConversationKey conversation,
        CancellationToken cancellationToken = default)
    {
        await _index.RunAsync();
        var found = await _turns
            .Find(d => d.Run == run && d.Mode == conversation.Mode && d.Repeat == conversation.Repeat
                       && d.Task == conversation.Task)
            .Project<TurnDocument>(Builders<TurnDocument>.Projection.Exclude(d => d.Events)).SortBy(d => d.Turn)
            .ToListAsync(cancellationToken);
        return [.. found.Select(d => d.ToTurn())];
    }

    /// <summary>A turn's stream events in the order they came; none for a turn it doesn't know.</summary>
    public async Task<IReadOnlyList<StreamEvent>> TurnEventsAsync(string run, TurnKey turn,
        CancellationToken cancellationToken = default)
    {
        await _index.RunAsync();
        // Only the events come back from Mongo; the rest of the turn stays where it is.
        var found = await _turns.Find(ById(TurnIdDocument.Of(run, turn)))
            .Project<TurnDocument>(Builders<TurnDocument>.Projection.Include(d => d.Events))
            .FirstOrDefaultAsync(cancellationToken);
        return found?.ToEvents() ?? [];
    }

    /// <summary>Deletes the run's turns, events and all, and says whether there were any.</summary>
    public async Task<bool> DeleteTurnsAsync(string run, CancellationToken cancellationToken = default)
    {
        await _index.RunAsync();
        return (await _turns.DeleteManyAsync(d => d.Run == run, cancellationToken)).DeletedCount > 0;
    }

    /// <summary>Finds the turn under this id.</summary>
    private static FilterDefinition<TurnDocument> ById(TurnIdDocument id) =>
        Builders<TurnDocument>.Filter.Eq(d => d.Id, id);
}
