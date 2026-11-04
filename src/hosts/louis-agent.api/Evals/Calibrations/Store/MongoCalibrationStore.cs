using LouisAgent.Api.Evals.Store;
using LouisAgent.Api.Evals.Store.Mongo;
using MongoDB.Driver;

namespace LouisAgent.Api.Evals.Calibrations.Store;

/// <summary>
/// Judge checks kept in MongoDB: one collection, <c>calibrations</c>, in the eval store's database, one document per
/// report. Reports are added, never updated.
/// </summary>
/// <remarks>
/// Repository over a typed collection (ADR 0048): the driver maps each report's <see cref="CalibrationDocument"/>,
/// and the filter, sort and index name their fields through the record. It opens the database as the eval store does
/// (<see cref="MongoEvalDatabase.Open"/>), with the same short timeouts and the same mappings. The index is made on
/// first use.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
public sealed class MongoCalibrationStore : ICalibrationStore
{
    /// <summary>The reports.</summary>
    private readonly IMongoCollection<CalibrationDocument> _reports;

    /// <summary>Makes the index the first time anything needs it, and tries again if that failed.</summary>
    private readonly OnceItWorks _index;

    /// <summary>Connects to the database, with short timeouts so a missing server fails fast.</summary>
    public MongoCalibrationStore(string url, string database)
    {
        _reports = MongoEvalDatabase.Open(url, database).GetCollection<CalibrationDocument>("calibrations");

        // A run's reports, newest first, is the one query; the index is made the first time a call works.
        _index = new OnceItWorks(() => _reports.Indexes.CreateOneAsync(new CreateIndexModel<CalibrationDocument>(
            Builders<CalibrationDocument>.IndexKeys.Ascending(d => d.Run).Descending(d => d.At))));
    }

    /// <summary>The database itself, for the tests to drop theirs.</summary>
    internal IMongoDatabase Database => _reports.Database;

    /// <summary>Writes the report as a new document.</summary>
    public async Task AddAsync(CalibrationRecord report, CancellationToken cancellationToken = default)
    {
        await _index.RunAsync();
        await _reports.InsertOneAsync(CalibrationDocument.Of(report), cancellationToken: cancellationToken);
    }

    /// <summary>Reads the run's reports back, newest first.</summary>
    public async Task<IReadOnlyList<CalibrationRecord>> OfRunAsync(string run,
        CancellationToken cancellationToken = default)
    {
        await _index.RunAsync();
        var found = await _reports.Find(d => d.Run == run).SortByDescending(d => d.At)
            .ToListAsync(cancellationToken);
        return [.. found.Select(d => d.ToRecord())];
    }

    /// <summary>Deletes the run's reports and says how many went.</summary>
    public async Task<int> DeleteRunAsync(string run, CancellationToken cancellationToken = default) =>
        (int)(await _reports.DeleteManyAsync(d => d.Run == run, cancellationToken)).DeletedCount;
}
