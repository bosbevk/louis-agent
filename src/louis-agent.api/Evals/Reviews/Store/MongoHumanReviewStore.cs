using LouisAgent.Api.Evals.Store;
using LouisAgent.Api.Evals.Store.Mongo;
using MongoDB.Driver;

namespace LouisAgent.Api.Evals.Reviews.Store;

/// <summary>
/// People's grades kept in MongoDB: one collection, <c>human_grades</c>, in the eval store's database, one document
/// per grade. Grades are added, never updated.
/// </summary>
/// <remarks>
/// Repository over a typed collection (ADR 0048): the driver maps each grade's <see cref="HumanGradeDocument"/>, its
/// verdict by its wire name, and the filter, sort and index name their fields through the record. It opens the
/// database as the eval store does (<see cref="MongoEvalDatabase.Open"/>), with the same short timeouts and the same
/// mappings. The index is made on first use.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
public sealed class MongoHumanReviewStore : IHumanReviewStore
{
    /// <summary>The grades.</summary>
    private readonly IMongoCollection<HumanGradeDocument> _grades;

    /// <summary>Makes the index the first time anything needs it, and tries again if that failed.</summary>
    private readonly OnceItWorks _index;

    /// <summary>Connects to the database, with short timeouts so a missing server fails fast.</summary>
    public MongoHumanReviewStore(string url, string database)
    {
        _grades = MongoEvalDatabase.Open(url, database).GetCollection<HumanGradeDocument>("human_grades");

        // A run's grades, oldest first, is the one query; the index is made the first time a call works.
        _index = new OnceItWorks(() => _grades.Indexes.CreateOneAsync(new CreateIndexModel<HumanGradeDocument>(
            Builders<HumanGradeDocument>.IndexKeys.Ascending(d => d.Run).Ascending(d => d.At))));
    }

    /// <summary>The database itself, for the tests to drop theirs.</summary>
    internal IMongoDatabase Database => _grades.Database;

    /// <summary>Writes the grade as a new document.</summary>
    public async Task AddAsync(HumanGrade grade, CancellationToken cancellationToken = default)
    {
        await _index.RunAsync();
        await _grades.InsertOneAsync(HumanGradeDocument.Of(grade), cancellationToken: cancellationToken);
    }

    /// <summary>Reads the run's grades back, oldest first.</summary>
    public async Task<IReadOnlyList<HumanGrade>> OfRunAsync(string run, CancellationToken cancellationToken = default)
    {
        await _index.RunAsync();
        var found = await _grades.Find(d => d.Run == run).SortBy(d => d.At).ToListAsync(cancellationToken);
        return [.. found.Select(d => d.ToGrade())];
    }

    /// <summary>Deletes the run's grades and says how many went.</summary>
    public async Task<int> DeleteRunAsync(string run, CancellationToken cancellationToken = default) =>
        (int)(await _grades.DeleteManyAsync(d => d.Run == run, cancellationToken)).DeletedCount;
}
