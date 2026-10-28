using LouisAgent.Api.Evals.Store;
using MongoDB.Bson;
using MongoDB.Driver;

namespace LouisAgent.Api.Evals.Calibrations.Store;

/// <summary>
/// Judge checks kept in MongoDB: one collection, <c>calibrations</c>, in the eval store's database, one document per
/// report. Reports are added, never updated.
/// </summary>
/// <remarks>
/// Mapped by hand in snake case, like the run store and the review store, so the three read alike. The index is made
/// on first use, and a server that doesn't answer fails a call within the timeout instead of hanging it.
/// </remarks>
public sealed class MongoCalibrationStore : ICalibrationStore
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private readonly IMongoCollection<BsonDocument> _reports;
    private readonly OnceItWorks _index;

    /// <summary>Connects to the database, with short timeouts so a missing server fails fast.</summary>
    public MongoCalibrationStore(string url, string database)
    {
        var settings = MongoClientSettings.FromConnectionString(url);
        settings.ServerSelectionTimeout = Timeout;
        settings.ConnectTimeout = Timeout;
        _reports = new MongoClient(settings).GetDatabase(database).GetCollection<BsonDocument>("calibrations");

        // A run's reports, newest first, is the one query; the index is made the first time a call works.
        _index = new OnceItWorks(() =>
            _reports.Indexes.CreateOneAsync(
                new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("run")
                    .Descending("at"))));
    }

    /// <summary>The database itself, for the tests to drop theirs.</summary>
    internal IMongoDatabase Database => _reports.Database;

    /// <summary>Writes the report as a new document.</summary>
    public async Task AddAsync(CalibrationRecord report, CancellationToken cancellationToken = default)
    {
        await _index.RunAsync();
        await _reports.InsertOneAsync(new BsonDocument
        {
            ["run"] = report.Run,
            ["grader"] = report.Grader,
            ["judge_model"] = report.JudgeModel is null ? BsonNull.Value : new BsonString(report.JudgeModel),
            ["at"] = new BsonDateTime(report.At.UtcDateTime),
            ["agreed"] = report.Agreed,
            ["total"] = report.Total,
            ["bar"] = report.Bar,
            ["passes"] = report.Passes,
            ["person_pass"] = Class(report.PersonPass),
            ["person_fail"] = Class(report.PersonFail),
            ["disagreements"] = new BsonArray(report.Disagreements.Select(d => new BsonDocument
            {
                ["scenario"] = d.Scenario,
                ["mode"] = d.Mode,
                ["repeat"] = d.Repeat,
                ["label"] = d.Label,
                ["judge"] = d.Judge,
                ["reason"] = d.Reason,
            })),
        }, cancellationToken: cancellationToken);
    }

    /// <summary>Reads the run's reports back, newest first.</summary>
    public async Task<IReadOnlyList<CalibrationRecord>> OfRunAsync(string run,
        CancellationToken cancellationToken = default)
    {
        await _index.RunAsync();
        var documents = await _reports.Find(Builders<BsonDocument>.Filter.Eq("run", run))
            .Sort(Builders<BsonDocument>.Sort.Descending("at")).ToListAsync(cancellationToken);

        // Each document back into a record, field by field; a stored null judge model stays null.
        return
        [
            .. documents.Select(d => new CalibrationRecord(d["run"].AsString, d["grader"].AsString,
                d["judge_model"].IsBsonNull ? null : d["judge_model"].AsString,
                new DateTimeOffset(d["at"].ToUniversalTime(), TimeSpan.Zero), d["agreed"].AsInt32, d["total"].AsInt32,
                d["bar"].AsInt32, d["passes"].AsBoolean,
                Class(d["person_pass"].AsBsonDocument), Class(d["person_fail"].AsBsonDocument),
                d["disagreements"].AsBsonArray.Select(v => v.AsBsonDocument).Select(x =>
                    new CalibrationDisagreement(x["scenario"].AsString, x["mode"].AsString, x["repeat"].AsInt32,
                        x["label"].AsString, x["judge"].AsString, x["reason"].AsString)).ToArray()))
        ];
    }

    /// <summary>Deletes the run's reports and says how many went.</summary>
    public async Task<int> DeleteRunAsync(string run, CancellationToken cancellationToken = default) =>
        (int)(await _reports.DeleteManyAsync(Builders<BsonDocument>.Filter.Eq("run", run), cancellationToken))
        .DeletedCount;

    /// <summary>One class's counts as a small document.</summary>
    private static BsonDocument Class(CalibrationClassCounts c) => new()
    {
        ["judge_pass"] = c.JudgePass,
        ["judge_fail"] = c.JudgeFail,
        ["judge_unknown"] = c.JudgeUnknown,
    };

    /// <summary>One class's counts read back from its document.</summary>
    private static CalibrationClassCounts Class(BsonDocument d) => new(d["judge_pass"].AsInt32, d["judge_fail"].AsInt32,
        d["judge_unknown"].AsInt32);
}
