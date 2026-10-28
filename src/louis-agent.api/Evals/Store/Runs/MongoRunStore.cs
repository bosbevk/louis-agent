using LouisAgent.Api.Evals.Runs.Kinds;
using LouisAgent.Api.Evals.Store.Mongo;
using LouisAgent.Api.Evals.Store.Mongo.Documents;
using MongoDB.Bson;
using MongoDB.Driver;
using static LouisAgent.Api.Evals.Store.EvalDocuments;
using static LouisAgent.Api.Evals.Store.Mongo.BsonValues;
using static LouisAgent.Api.Evals.Store.Mongo.MongoEvalDatabase;

namespace LouisAgent.Api.Evals.Store.Runs;

/// <summary>
/// Keeps the runs in MongoDB's <c>runs</c> collection, one document per run. Its index (by check, newest first) is
/// made the first time anything needs it, so starting the API never waits on Mongo.
/// </summary>
/// <remarks>
/// Repository, with a Data Mapper (<see cref="RunDocuments"/>) written by hand, so what <c>mongosh</c> shows is what
/// the portal reads. This class keeps only the collection and its queries.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0022-benchmarks-recorded-from-the-protocol.md">ADR 0022: benchmark results in MongoDB</seealso>
public sealed class MongoRunStore : IRunStore
{
    /// <summary>The runs.</summary>
    private readonly IMongoCollection<BsonDocument> _runs;

    /// <summary>Makes the index the first time anything needs it, and tries again if that failed.</summary>
    private readonly OnceItWorks _index;

    /// <summary>Uses the database's runs collection. Nothing is created until the first call.</summary>
    public MongoRunStore(IMongoDatabase database)
    {
        _runs = database.GetCollection<BsonDocument>("runs");
        _index = new OnceItWorks(() => _runs.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("check").Descending("started_at"))));
    }

    /// <summary>Saves the run, replacing the one under its id.</summary>
    public async Task SaveRunAsync(Run run, CancellationToken cancellationToken = default)
    {
        await _index.RunAsync();
        await _runs.ReplaceOneAsync(ById(run.Id), RunDocuments.ToBson(run), Upsert, cancellationToken);
    }

    /// <summary>Finds a run by its id.</summary>
    public async Task<Run?> GetRunAsync(string id, CancellationToken cancellationToken = default)
    {
        await _index.RunAsync();
        var found = await _runs.Find(ById(id)).FirstOrDefaultAsync(cancellationToken);
        return found is null ? null : RunDocuments.RunOf(found);
    }

    /// <summary>The newest runs first, up to the limit, and one eval's only if asked.</summary>
    public async Task<IReadOnlyList<Run>> ListRunsAsync(string? check = null, int limit = 50,
        CancellationToken cancellationToken = default)
    {
        await _index.RunAsync();
        var filter = check is null
            ? Builders<BsonDocument>.Filter.Empty
            : Builders<BsonDocument>.Filter.Eq("check", check);
        var found = await _runs.Find(filter).Sort(Builders<BsonDocument>.Sort.Descending("started_at")).Limit(limit)
            .ToListAsync(cancellationToken);
        return found.Select(RunDocuments.RunOf).ToArray();
    }

    /// <summary>Marks every run still running as failed and ended now, and says how many there were.</summary>
    public async Task<int> CloseOrphansAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        await _index.RunAsync();
        var result = await _runs.UpdateManyAsync(
            Builders<BsonDocument>.Filter.Eq("status", RunDocuments.Status(EvalRunStatus.Running)),
            Builders<BsonDocument>.Update.Set("status", RunDocuments.Status(EvalRunStatus.Failed))
                .Set("ended_at", Date(now)),
            cancellationToken: cancellationToken);
        return (int)result.ModifiedCount;
    }

    /// <summary>Deletes the run's document, and says whether there was one.</summary>
    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        await _index.RunAsync();
        return (await _runs.DeleteOneAsync(ById(id), cancellationToken)).DeletedCount > 0;
    }
}
