using LouisAgent.Api.Evals.Store;
using LouisAgent.Api.Evals.Store.Mongo;
using LouisAgent.Choices;
using MongoDB.Bson;
using MongoDB.Driver;
using static LouisAgent.Api.Evals.Runs.Protocol.EvalProtocol;

namespace LouisAgent.Api.Evals.Reviews.Store;

/// <summary>
/// People's grades kept in MongoDB: one collection, <c>human_grades</c>, in the eval store's database, one document
/// per grade. Grades are added, never updated.
/// </summary>
/// <remarks>
/// Mapped by hand in snake case like the run store (<see cref="MongoEvalStore"/>). That class is already over its
/// size, so this is a class beside it rather than more of it. The index is made on first use, and a server that
/// doesn't answer fails a call within the timeout, as the run store's does.
/// </remarks>
public sealed class MongoHumanReviewStore : IHumanReviewStore
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private readonly IMongoCollection<BsonDocument> _grades;
    private readonly OnceItWorks _index;

    /// <summary>Connects to the database, with short timeouts so a missing server fails fast.</summary>
    public MongoHumanReviewStore(string url, string database)
    {
        var settings = MongoClientSettings.FromConnectionString(url);
        settings.ServerSelectionTimeout = Timeout;
        settings.ConnectTimeout = Timeout;
        _grades = new MongoClient(settings).GetDatabase(database).GetCollection<BsonDocument>("human_grades");

        // A run's grades, oldest first, is the one query; the index is made the first time a call works.
        _index = new OnceItWorks(() =>
            _grades.Indexes.CreateOneAsync(
                new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("run").Ascending("at"))));
    }

    /// <summary>The database itself, for the tests to drop theirs.</summary>
    internal IMongoDatabase Database => _grades.Database;

    /// <summary>Writes the grade as a new document, its verdict by its wire name.</summary>
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

    /// <summary>Reads the run's grades back, oldest first.</summary>
    public async Task<IReadOnlyList<HumanGrade>> OfRunAsync(string run, CancellationToken cancellationToken = default)
    {
        await _index.RunAsync();
        var documents = await _grades.Find(Builders<BsonDocument>.Filter.Eq("run", run))
            .Sort(Builders<BsonDocument>.Sort.Ascending("at")).ToListAsync(cancellationToken);

        // Each document back into a grade, field by field; the verdict is read whatever its case.
        return
        [
            .. documents.Select(d => new HumanGrade(d["run"].AsString,
                new ConversationKey(d["mode"].AsString, d["repeat"].AsInt32, d["task"].AsString), d["grader"].AsString,
                Enum.Parse<HumanVerdict>(d["verdict"].AsString, ignoreCase: true), d["note"].AsString,
                d["reviewer"].AsString,
                new DateTimeOffset(d["at"].ToUniversalTime(), TimeSpan.Zero)))
        ];
    }

    /// <summary>Deletes the run's grades and says how many went.</summary>
    public async Task<int> DeleteRunAsync(string run, CancellationToken cancellationToken = default) =>
        (int)(await _grades.DeleteManyAsync(Builders<BsonDocument>.Filter.Eq("run", run), cancellationToken))
        .DeletedCount;
}
