using MongoDB.Driver;
using static LouisAgent.Api.Evals.Store.EvalDocuments;
using static LouisAgent.Api.Evals.Store.Mongo.MongoEvalDatabase;

namespace LouisAgent.Api.Evals.Store.Requests;

/// <summary>
/// Keeps the model requests in MongoDB's <c>requests</c> collection, one document per request, tagged with its run.
/// Its index (by run, by time) is made the first time anything needs it, so starting the API never waits on Mongo.
/// </summary>
/// <remarks>
/// Repository over a typed collection (ADR 0048): the driver maps each request's <see cref="RequestDocument"/>, and
/// every filter, sort and index names its field through the record, so a renamed field fails the build. This class
/// keeps only the collection and its queries.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0007-measure-every-request.md">ADR 0007: measure every request</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0022-benchmarks-recorded-from-the-protocol.md">ADR 0022: benchmark results in MongoDB</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
public sealed class MongoRequestStore : IRequestStore
{
    /// <summary>The ledger's requests.</summary>
    private readonly IMongoCollection<RequestDocument> _requests;

    /// <summary>Makes the index the first time anything needs it, and tries again if that failed.</summary>
    private readonly OnceItWorks _index;

    /// <summary>Uses the database's requests collection. Nothing is created until the first call.</summary>
    public MongoRequestStore(IMongoDatabase database)
    {
        // The driver must know how enums and times are stored before it maps its first document.
        RegisterMappings();
        _requests = database.GetCollection<RequestDocument>("requests");
        _index = new OnceItWorks(() => _requests.Indexes.CreateOneAsync(new CreateIndexModel<RequestDocument>(
            Builders<RequestDocument>.IndexKeys.Ascending(d => d.Run).Ascending(d => d.At))));
    }

    /// <summary>Adds the ledger's requests, tagged with their run.</summary>
    public async Task AddRequestsAsync(string run, IReadOnlyList<Request> requests,
        CancellationToken cancellationToken = default)
    {
        // Mongo refuses an empty insert, and there's nothing to add anyway.
        if (requests.Count == 0) return;

        await _index.RunAsync();
        await _requests.InsertManyAsync(requests.Select(r => RequestDocument.Of(r with { Run = run })),
            cancellationToken: cancellationToken);
    }

    /// <summary>The run's requests, by time.</summary>
    public async Task<IReadOnlyList<Request>> RequestsAsync(string run, CancellationToken cancellationToken = default)
    {
        await _index.RunAsync();
        var found = await _requests.Find(d => d.Run == run).SortBy(d => d.At).ToListAsync(cancellationToken);
        return [.. found.Select(d => d.ToRequest())];
    }

    /// <summary>Deletes the run's requests, and says whether there were any.</summary>
    public async Task<bool> DeleteRequestsAsync(string run, CancellationToken cancellationToken = default)
    {
        await _index.RunAsync();
        return (await _requests.DeleteManyAsync(d => d.Run == run, cancellationToken)).DeletedCount > 0;
    }
}
