using LouisAgent.Packs.ShopPrices.Prices.Files;
using MongoDB.Driver;

namespace LouisAgent.Packs.ShopPrices.Prices.Store;

/// <summary>
/// Keeps the price tables in MongoDB, one document per source and month. The first read adds each table the pack
/// ships that the store doesn't have yet, so an empty store starts with them and a newly shipped month joins the
/// history; then each source's newest month is read. An upload saves a month, or replaces it with a correction.
/// </summary>
/// <remarks>
/// A strategy of <see cref="IPriceStore"/> (Gang of Four) over a typed collection (ADR 0048). A table already stored
/// is never overwritten by a seed: the store is the record, the shipped files only seed it, and only an upload a
/// person saved replaces a month (F55).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
public sealed class MongoPriceStore : IPriceStore
{
    /// <summary>The collection the tables are kept in.</summary>
    public const string Collection = "price_files";

    /// <summary>How long a call waits for the server before giving up, so a tool call never hangs.</summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private readonly IMongoCollection<PriceFileDocument> _files;
    private readonly IReadOnlyList<PriceFile> _shipped;
    private readonly SemaphoreSlim _seeding = new(1, 1);
    private bool _seeded;

    /// <summary>A store over this collection, seeded from these tables, which also settle a tie's order.</summary>
    public MongoPriceStore(IMongoCollection<PriceFileDocument> files, IReadOnlyList<PriceFile> shipped)
    {
        _files = files;
        _shipped = shipped;
    }

    /// <summary>The store on the server at the address, seeded from the shipped tables. Nothing is contacted yet.</summary>
    public static MongoPriceStore Open(string url, string database)
    {
        var settings = MongoClientSettings.FromConnectionString(url);
        settings.ServerSelectionTimeout = Timeout;
        settings.ConnectTimeout = Timeout;
        var files = new MongoClient(settings).GetDatabase(database).GetCollection<PriceFileDocument>(Collection);
        return new MongoPriceStore(files, PriceBook.Default.Files);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PriceFile>> LatestAsync(CancellationToken cancellationToken)
    {
        await SeedOnceAsync(cancellationToken);
        var all = await _files.Find(FilterDefinition<PriceFileDocument>.Empty)
            .SortByDescending(d => d.AsOf).ToListAsync(cancellationToken);

        // The newest month of each source comes first in the sort, so the first one per source is kept.
        return
        [
            .. all.GroupBy(d => d.Source).Select(source => source.First().ToFile())
                .OrderBy(file => Rank(file.Source)).ThenBy(file => file.Source, StringComparer.Ordinal)
        ];
    }

    /// <inheritdoc />
    public bool CanSave => true;

    /// <inheritdoc />
    public async Task<IReadOnlyList<PriceFile>> MonthsAsync(string source, CancellationToken cancellationToken)
    {
        await SeedOnceAsync(cancellationToken);
        var months = await _files.Find(d => d.Source == source).SortBy(d => d.AsOf).ToListAsync(cancellationToken);
        return [.. months.Select(d => d.ToFile())];
    }

    /// <inheritdoc />
    public async Task SaveAsync(PriceFile file, CancellationToken cancellationToken)
    {
        // Seeded first, so a shipped month never lands later on top of the one saved here.
        await SeedOnceAsync(cancellationToken);
        var document = PriceFileDocument.Of(file);
        await _files.ReplaceOneAsync(d => d.Id == document.Id, document, new ReplaceOptions { IsUpsert = true },
            cancellationToken);
    }

    /// <summary>
    /// Seeds the store the first time it is read; a seed that failed (the server was down) is tried again on the next
    /// read.
    /// </summary>
    private async Task SeedOnceAsync(CancellationToken cancellationToken)
    {
        if (_seeded) return;
        await _seeding.WaitAsync(cancellationToken);
        try
        {
            // Another read may have seeded it while this one waited.
            if (_seeded) return;
            await SeedAsync(cancellationToken);
            _seeded = true;
        }
        finally
        {
            _seeding.Release();
        }
    }

    /// <summary>Adds each shipped table the store doesn't hold yet; one already stored is left as it is.</summary>
    private async Task SeedAsync(CancellationToken cancellationToken)
    {
        foreach (var document in _shipped.Select(PriceFileDocument.Of))
        {
            if (await _files.CountDocumentsAsync(d => d.Id == document.Id, cancellationToken: cancellationToken) > 0)
                continue;
            await _files.InsertOneAsync(document, cancellationToken: cancellationToken);
        }
    }

    /// <summary>A source's place in a tie: the shipped order (Stats SA's first), any other source after them.</summary>
    private int Rank(string source)
    {
        int index = _shipped.Select(file => file.Source).ToList().IndexOf(source);
        return index < 0 ? _shipped.Count : index;
    }
}
