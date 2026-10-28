using LouisAgent.Api.Evals.Runs.Kinds;
using LouisAgent.Api.Evals.Store.Mongo;
using MongoDB.Driver;
using static LouisAgent.Api.Evals.Store.EvalDocuments;
using static LouisAgent.Api.Evals.Store.Mongo.MongoEvalDatabase;

namespace LouisAgent.Api.Evals.Store.Runs;

/// <summary>
/// Keeps the runs in MongoDB's <c>runs</c> collection, one document per run. Its index (by check, newest first) is
/// made the first time anything needs it, so starting the API never waits on Mongo.
/// </summary>
/// <remarks>
/// Repository over a typed collection (ADR 0048): the driver maps each run's <see cref="RunDocument"/>, and every
/// filter, sort and index names its field through the record, so a renamed field fails the build. This class keeps
/// only the collection and its queries.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0022-benchmarks-recorded-from-the-protocol.md">ADR 0022: benchmark results in MongoDB</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
public sealed class MongoRunStore : IRunStore
{
    /// <summary>The runs.</summary>
    private readonly IMongoCollection<RunDocument> _runs;

    /// <summary>Makes the index the first time anything needs it, and tries again if that failed.</summary>
    private readonly OnceItWorks _index;

    /// <summary>Uses the database's runs collection. Nothing is created until the first call.</summary>
    public MongoRunStore(IMongoDatabase database)
    {
        // The driver must know how enums and times are stored before it maps its first document.
        RegisterMappings();
        _runs = database.GetCollection<RunDocument>("runs");
        _index = new OnceItWorks(() => _runs.Indexes.CreateOneAsync(new CreateIndexModel<RunDocument>(
            Builders<RunDocument>.IndexKeys.Ascending(d => d.Check).Descending(d => d.StartedAt))));
    }

    /// <summary>Saves the run, replacing the one under its id.</summary>
    public async Task SaveRunAsync(Run run, CancellationToken cancellationToken = default)
    {
        await _index.RunAsync();
        await _runs.ReplaceOneAsync(d => d.Id == run.Id, RunDocument.Of(run), Upsert, cancellationToken);
    }

    /// <summary>Finds a run by its id.</summary>
    public async Task<Run?> GetRunAsync(string id, CancellationToken cancellationToken = default)
    {
        await _index.RunAsync();
        var found = await _runs.Find(d => d.Id == id).FirstOrDefaultAsync(cancellationToken);
        return found?.ToRun();
    }

    /// <summary>The newest runs first, up to the limit, and one eval's only if asked.</summary>
    public async Task<IReadOnlyList<Run>> ListRunsAsync(string? check = null, int limit = 50,
        CancellationToken cancellationToken = default)
    {
        await _index.RunAsync();
        var filter = check is null
            ? Builders<RunDocument>.Filter.Empty
            : Builders<RunDocument>.Filter.Eq(d => d.Check, check);
        var found = await _runs.Find(filter).SortByDescending(d => d.StartedAt).Limit(limit)
            .ToListAsync(cancellationToken);
        return [.. found.Select(d => d.ToRun())];
    }

    /// <summary>Marks every run still running as failed and ended now, and says how many there were.</summary>
    public async Task<int> CloseOrphansAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        await _index.RunAsync();
        var result = await _runs.UpdateManyAsync(d => d.Status == EvalRunStatus.Running,
            Builders<RunDocument>.Update.Set(d => d.Status, EvalRunStatus.Failed).Set(d => d.EndedAt, now),
            cancellationToken: cancellationToken);
        return (int)result.ModifiedCount;
    }

    /// <summary>Deletes the run's document, and says whether there was one.</summary>
    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        await _index.RunAsync();
        return (await _runs.DeleteOneAsync(d => d.Id == id, cancellationToken)).DeletedCount > 0;
    }
}
