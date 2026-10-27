using LouisAgent.Choices;
using LouisAgent.Api.Benchmarks.Store;
using MongoDB.Bson;
using MongoDB.Driver;
using static LouisAgent.Api.Benchmarks.Runs.BenchmarkProtocol;

namespace LouisAgent.Api.Benchmarks.Reviews.Store;

/// <summary>
/// People's grades in MongoDB (F18 step 6a.2): one collection, <c>human_grades</c>, in the benchmark store's database, a document per grade, mapped by hand in snake case like the
/// benchmark store's (<see cref="MongoBenchmarkStore"/>, which is over its size and so is not made longer: this is a class beside it). Grades are added, never updated. The index is made on
/// first use; a server that doesn't answer fails a call within the timeout, as the benchmark store's does.
/// </summary>
public sealed class MongoHumanReviewStore : IHumanReviewStore
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private readonly IMongoCollection<BsonDocument> _grades;
    private readonly OnceItWorks _index;

    public MongoHumanReviewStore(string url, string database)
    {
        var settings = MongoClientSettings.FromConnectionString(url);
        settings.ServerSelectionTimeout = Timeout;
        settings.ConnectTimeout = Timeout;
        _grades = new MongoClient(settings).GetDatabase(database).GetCollection<BsonDocument>("human_grades");
        _index = new OnceItWorks(() =>
            _grades.Indexes.CreateOneAsync(
                new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("run").Ascending("at"))));
    }

    /// <summary>The database itself, for the tests to drop theirs.</summary>
    internal IMongoDatabase Database => _grades.Database;

    public async Task AddAsync(HumanGrade grade, CancellationToken cancellationToken = default)
    {
        await _index.RunAsync();
        await _grades.InsertOneAsync(new BsonDocument
        {
            ["run"] = grade.Run,
            ["mode"] = grade.Conversation.Mode,
            ["repeat"] = grade.Conversation.Repeat,
            ["task"] = grade.Conversation.Task,
            ["grader"] = grade.Grader,
            ["verdict"] = grade.Verdict.Wire(),
            ["note"] = grade.Note,
            ["reviewer"] = grade.Reviewer,
            ["at"] = new BsonDateTime(grade.At.UtcDateTime),
        }, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<HumanGrade>> OfRunAsync(string run, CancellationToken cancellationToken = default)
    {
        await _index.RunAsync();
        var documents = await _grades.Find(Builders<BsonDocument>.Filter.Eq("run", run))
            .Sort(Builders<BsonDocument>.Sort.Ascending("at")).ToListAsync(cancellationToken);
        return
        [
            .. documents.Select(d => new HumanGrade(d["run"].AsString,
                new ConversationKey(d["mode"].AsString, d["repeat"].AsInt32, d["task"].AsString), d["grader"].AsString,
                Enum.Parse<HumanVerdict>(d["verdict"].AsString, ignoreCase: true), d["note"].AsString,
                d["reviewer"].AsString,
                new DateTimeOffset(d["at"].ToUniversalTime(), TimeSpan.Zero)))
        ];
    }

    public async Task<int> DeleteRunAsync(string run, CancellationToken cancellationToken = default) =>
        (int)(await _grades.DeleteManyAsync(Builders<BsonDocument>.Filter.Eq("run", run), cancellationToken))
        .DeletedCount;
}
