using MongoDB.Driver;

namespace LouisAgent.Packs.Recipes.Library.Usage;

/// <summary>
/// Keeps the library's counter in MongoDB, one record per UTC day, next to the recipes. Each add is one small update on
/// the server, so two cooks counting at once can't lose each other's number.
/// </summary>
/// <remarks>
/// A strategy of <see cref="IRecipeUsage"/> (Gang of Four) over a typed collection (ADR 0048). <c>$inc</c> with upsert
/// on the id is atomic, and the integration test holds that many adds at once on a new day all count, on the MongoDB 8
/// we run. There's no retry here: with one switched off that test still passed, so it would be code nothing needs.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0048-mongo-through-records.md">ADR 0048: Mongo through records</seealso>
public sealed class MongoRecipeUsage(IMongoCollection<RecipeUsageDocument> days) : IRecipeUsage
{
    /// <summary>The collection the counter is kept in.</summary>
    public const string Collection = "library_usage";

    /// <summary>How long a call waits for the server before giving up, so a count never holds up a tool.</summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    /// <summary>The counter on the server at the address. Nothing is contacted yet.</summary>
    public static MongoRecipeUsage Open(string url, string database)
    {
        var settings = MongoClientSettings.FromConnectionString(url);
        settings.ServerSelectionTimeout = Timeout;
        settings.ConnectTimeout = Timeout;
        return new MongoRecipeUsage(
            new MongoClient(settings).GetDatabase(database).GetCollection<RecipeUsageDocument>(Collection));
    }

    /// <inheritdoc />
    public Task AddAsync(RecipeUsageKind kind, long by, DateOnly day, CancellationToken cancellationToken) =>
        days.UpdateOneAsync(record => record.Id == RecipeUsageDocument.IdOf(day), Increment(kind, by),
            new UpdateOptions { IsUpsert = true }, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecipeUsageDay>> DaysAsync(CancellationToken cancellationToken)
    {
        var records = await days.Find(FilterDefinition<RecipeUsageDocument>.Empty)
            .SortByDescending(record => record.Id).ToListAsync(cancellationToken);
        return [.. records.Select(record => record.ToDay())];
    }

    /// <summary>The update for one kind, in a table so each kind's field is named once.</summary>
    /// <remarks>A switch expression on the kind (ADR 0051), typed so a renamed field can't drift (ADR 0048).</remarks>
    private static UpdateDefinition<RecipeUsageDocument> Increment(RecipeUsageKind kind, long by)
    {
        var update = Builders<RecipeUsageDocument>.Update;
        return kind switch
        {
            RecipeUsageKind.Kept => update.Inc(record => record.Kept, by),
            RecipeUsageKind.Offered => update.Inc(record => record.Offered, by),
            RecipeUsageKind.Served => update.Inc(record => record.Served, by),
            RecipeUsageKind.Failed => update.Inc(record => record.Failed, by),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }
}
