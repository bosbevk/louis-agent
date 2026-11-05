using MongoDB.Driver;

namespace LouisAgent.Packs.Recipes.Library.Store;

/// <summary>
/// Keeps the recipes in MongoDB, one document per page, for good. A page already there stays exactly as it was first read,
/// so the first copy is the one we keep, even when two cooks read it at the same moment.
/// </summary>
/// <remarks>
/// A strategy of <see cref="IRecipeStore"/> (Gang of Four) over a typed collection (ADR 0048). Keeping relies on the
/// document's id, the page's one spelling, instead of looking first: asking the database to refuse a second copy can't race.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0048-mongo-through-records.md">ADR 0048: Mongo through records</seealso>
public sealed class MongoRecipeStore(IMongoCollection<StoredRecipeDocument> recipes) : IRecipeStore
{
    /// <summary>The collection the recipes are kept in.</summary>
    public const string Collection = "recipes";

    /// <summary>How long a call waits for the server before giving up, so a tool call never hangs.</summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    /// <summary>The store on the server at the address. Nothing is contacted yet.</summary>
    public static MongoRecipeStore Open(string url, string database)
    {
        var settings = MongoClientSettings.FromConnectionString(url);
        settings.ServerSelectionTimeout = Timeout;
        settings.ConnectTimeout = Timeout;
        return new MongoRecipeStore(
            new MongoClient(settings).GetDatabase(database).GetCollection<StoredRecipeDocument>(Collection));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StoredRecipe>> AllAsync(CancellationToken cancellationToken)
    {
        var all = await recipes.Find(FilterDefinition<StoredRecipeDocument>.Empty)
            .SortBy(document => document.ReadAt).ToListAsync(cancellationToken);
        return [.. all.Select(document => document.ToRecipe())];
    }

    /// <inheritdoc />
    public async Task KeepAsync(StoredRecipe recipe, CancellationToken cancellationToken)
    {
        try
        {
            await recipes.InsertOneAsync(StoredRecipeDocument.Of(recipe), cancellationToken: cancellationToken);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // We already hold this page, so the first copy stays. That's the point, not a failure.
        }
    }

    /// <inheritdoc />
    public Task PurgeSiteAsync(string slug, CancellationToken cancellationToken) =>
        recipes.DeleteManyAsync(document => document.Site == slug, cancellationToken);
}
