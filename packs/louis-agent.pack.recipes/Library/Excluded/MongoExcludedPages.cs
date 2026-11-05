using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace LouisAgent.Packs.Recipes.Library.Excluded;

/// <summary>
/// Keeps the excluded pages in MongoDB, one document per page, next to the recipes. A page already there stays as it
/// was, so two cooks finding the same page at once can't fail each other.
/// </summary>
/// <remarks>A strategy of <see cref="IExcludedPages"/> (Gang of Four) over a typed collection (ADR 0048).</remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0048-mongo-through-records.md">ADR 0048: Mongo through records</seealso>
public sealed class MongoExcludedPages(IMongoCollection<ExcludedPageDocument> pages) : IExcludedPages
{
    /// <summary>The collection the list is kept in.</summary>
    public const string Collection = "excluded_pages";

    /// <summary>How long a call waits for the server before giving up, so a tool call never hangs.</summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    /// <summary>The list on the server at the address. Nothing is contacted yet.</summary>
    public static MongoExcludedPages Open(string url, string database)
    {
        var settings = MongoClientSettings.FromConnectionString(url);
        settings.ServerSelectionTimeout = Timeout;
        settings.ConnectTimeout = Timeout;
        return new MongoExcludedPages(
            new MongoClient(settings).GetDatabase(database).GetCollection<ExcludedPageDocument>(Collection));
    }

    /// <inheritdoc />
    public async Task<IReadOnlySet<string>> IdsAsync(CancellationToken cancellationToken)
    {
        var ids = await pages.Find(FilterDefinition<ExcludedPageDocument>.Empty)
            .Project(document => document.Id).ToListAsync(cancellationToken);
        return ids.ToHashSet();
    }

    /// <inheritdoc />
    public async Task AddAsync(ExcludedPage page, CancellationToken cancellationToken)
    {
        try
        {
            await pages.InsertOneAsync(ExcludedPageDocument.Of(page), cancellationToken: cancellationToken);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // It's on the list already, which is all we wanted.
        }
    }
}

/// <summary>One excluded page as MongoDB keeps it. Its id comes from the page's one spelling, so it's listed once.
/// </summary>
/// <remarks>Metadata Mapping on a Data Transfer Object (Fowler, PoEAA); the date is a UTC BSON date (ADR 0046).
/// </remarks>
[BsonIgnoreExtraElements]
public sealed record ExcludedPageDocument(
    [property: BsonId] string Id,
    [property: BsonElement("page")] string Page,
    [property: BsonElement("at")] DateTime At)
{
    /// <summary>The document for a page.</summary>
    public static ExcludedPageDocument Of(ExcludedPage page) => new(page.Id, page.Page.AbsoluteUri, page.At.UtcDateTime);
}
