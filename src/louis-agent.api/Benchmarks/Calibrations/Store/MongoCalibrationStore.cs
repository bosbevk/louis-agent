using LouisAgent.Api.Benchmarks.Store;
using MongoDB.Bson;
using MongoDB.Driver;

namespace LouisAgent.Api.Benchmarks.Calibrations.Store;

/// <summary>
/// Judge checks in MongoDB (F18 step 6a.5b): one collection, <c>calibrations</c>, in the benchmark store's database, a document per report, mapped by hand in snake case like the
/// benchmark store's and the review store's. Reports are added, never updated. The index is made on first use; a server that doesn't answer fails a call within the timeout.
/// </summary>
public sealed class MongoCalibrationStore : ICalibrationStore
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private readonly IMongoCollection<BsonDocument> _reports;
    private readonly OnceItWorks _index;

    public MongoCalibrationStore(string url, string database)
    {
        var settings = MongoClientSettings.FromConnectionString(url);
        settings.ServerSelectionTimeout = Timeout;
        settings.ConnectTimeout = Timeout;
        _reports = new MongoClient(settings).GetDatabase(database).GetCollection<BsonDocument>("calibrations");
        _index = new OnceItWorks(() => _reports.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("run").Descending("at"))));
    }

    /// <summary>The database itself, for the tests to drop theirs.</summary>
    internal IMongoDatabase Database => _reports.Database;

    public async Task AddAsync(CalibrationRecord report, CancellationToken cancellationToken = default)
    {
        await _index.RunAsync();
        await _reports.InsertOneAsync(new BsonDocument
        {
            ["run"] = report.Run, ["grader"] = report.Grader, ["judge_model"] = report.JudgeModel is null ? BsonNull.Value : new BsonString(report.JudgeModel),
            ["at"] = new BsonDateTime(report.At.UtcDateTime), ["agreed"] = report.Agreed, ["total"] = report.Total, ["bar"] = report.Bar, ["passes"] = report.Passes,
            ["person_pass"] = Class(report.PersonPass), ["person_fail"] = Class(report.PersonFail),
            ["disagreements"] = new BsonArray(report.Disagreements.Select(d => new BsonDocument
            {
                ["scenario"] = d.Scenario, ["mode"] = d.Mode, ["repeat"] = d.Repeat, ["label"] = d.Label, ["judge"] = d.Judge, ["reason"] = d.Reason,
            })),
        }, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<CalibrationRecord>> OfRunAsync(string run, CancellationToken cancellationToken = default)
    {
        await _index.RunAsync();
        var documents = await _reports.Find(Builders<BsonDocument>.Filter.Eq("run", run)).Sort(Builders<BsonDocument>.Sort.Descending("at")).ToListAsync(cancellationToken);
        return [.. documents.Select(d => new CalibrationRecord(d["run"].AsString, d["grader"].AsString, d["judge_model"].IsBsonNull ? null : d["judge_model"].AsString,
            new DateTimeOffset(d["at"].ToUniversalTime(), TimeSpan.Zero), d["agreed"].AsInt32, d["total"].AsInt32, d["bar"].AsInt32, d["passes"].AsBoolean,
            Class(d["person_pass"].AsBsonDocument), Class(d["person_fail"].AsBsonDocument),
            d["disagreements"].AsBsonArray.Select(v => v.AsBsonDocument).Select(x => new CalibrationDisagreement(x["scenario"].AsString, x["mode"].AsString, x["repeat"].AsInt32,
                x["label"].AsString, x["judge"].AsString, x["reason"].AsString)).ToArray()))];
    }

    public async Task<int> DeleteRunAsync(string run, CancellationToken cancellationToken = default) =>
        (int)(await _reports.DeleteManyAsync(Builders<BsonDocument>.Filter.Eq("run", run), cancellationToken)).DeletedCount;

    private static BsonDocument Class(CalibrationClassCounts c) => new() { ["judge_pass"] = c.JudgePass, ["judge_fail"] = c.JudgeFail, ["judge_unknown"] = c.JudgeUnknown };

    private static CalibrationClassCounts Class(BsonDocument d) => new(d["judge_pass"].AsInt32, d["judge_fail"].AsInt32, d["judge_unknown"].AsInt32);
}
