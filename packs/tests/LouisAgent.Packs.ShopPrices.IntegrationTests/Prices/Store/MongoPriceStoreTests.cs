using LouisAgent.Packs.ShopPrices.Prices;
using LouisAgent.Packs.ShopPrices.Prices.Files;
using LouisAgent.Packs.ShopPrices.Prices.Store;
using MongoDB.Driver;

namespace LouisAgent.Packs.ShopPrices.IntegrationTests.Prices.Store;

/// <summary>
/// The price store against a real MongoDB, when <c>BENCHMARKS_MONGO_URL</c> is set (CI's throwaway server): an empty
/// store is seeded from the shipped tables, each month is kept, the newest is read, a stored table is never
/// overwritten by a seed, and an upload saves or corrects a month. Each test gets a database of its own, dropped at the end.
/// </summary>
[Category("Integration")]
public class MongoPriceStoreTests
{
    /// <summary>Where Mongo runs, if this machine has one.</summary>
    private static string? Url => Environment.GetEnvironmentVariable("BENCHMARKS_MONGO_URL");

    private string _database = null!;

    /// <summary>Skips the fixture without Mongo, and names a database only this test uses.</summary>
    [SetUp]
    public void NeedsMongo()
    {
        Outside.Tool(!string.IsNullOrEmpty(Url), "BENCHMARKS_MONGO_URL isn't set: the price store isn't tested here");
        _database = $"shop_prices_test_{Guid.NewGuid():N}";
    }

    /// <summary>Drops the test's database.</summary>
    [TearDown]
    public async Task DropTheTestDatabase()
    {
        if (string.IsNullOrEmpty(Url)) return;
        await new MongoClient(Url).DropDatabaseAsync(_database);
    }

    /// <summary>The test database's collection, as the store reads it.</summary>
    private IMongoCollection<PriceFileDocument> Files() =>
        new MongoClient(Url).GetDatabase(_database).GetCollection<PriceFileDocument>(MongoPriceStore.Collection);

    /// <summary>An empty store is seeded with the shipped tables and gives them back, Stats SA's first, unchanged.</summary>
    [Test]
    public async Task AnEmptyStore_IsSeeded_AndGivesTheShippedTablesBack()
    {
        var shipped = PriceBook.Default.Files;

        var latest = await MongoPriceStore.Open(Url!, _database).LatestAsync(CancellationToken.None);

        Assert.That(await Files().CountDocumentsAsync(FilterDefinition<PriceFileDocument>.Empty), Is.EqualTo(2));
        Assert.That(latest.Select(f => (f.Source, f.AsOf)), Is.EqualTo(shipped.Select(f => (f.Source, f.AsOf))));
        var pack = latest[0].Products.First(p => p.Product == "Beef mince").Packs[0];
        var shippedPack = shipped[0].Products.First(p => p.Product == "Beef mince").Packs[0];
        Assert.That((pack.Size, pack.Amount, pack.Unit), Is.EqualTo((shippedPack.Size, shippedPack.Amount, shippedPack.Unit)));
        Assert.That(pack.Prices, Is.EquivalentTo(shippedPack.Prices), "the prices come back to the cent");
    }

    /// <summary>A newer month of a source is read in place of the older one, which stays as history.</summary>
    [Test]
    public async Task ANewerMonth_IsRead_AndTheOlderOneIsKept()
    {
        var statsSa = PriceBook.Default.Files[0];
        var september = statsSa with { AsOf = new DateOnly(2026, 9, 1), Products = [statsSa.Products[0]] };
        await Files().InsertOneAsync(PriceFileDocument.Of(september));

        var latest = await MongoPriceStore.Open(Url!, _database).LatestAsync(CancellationToken.None);

        Assert.That(latest[0].AsOf, Is.EqualTo(new DateOnly(2026, 9, 1)), "Stats SA's newest month, still first");
        Assert.That(latest[0].Products, Has.Count.EqualTo(1));
        Assert.That(await Files().CountDocumentsAsync(FilterDefinition<PriceFileDocument>.Empty), Is.EqualTo(3),
            "August is seeded beside September, as history");
    }

    /// <summary>A table already stored is never overwritten by the shipped one of the same month.</summary>
    [Test]
    public async Task AStoredTable_IsNotOverwrittenByTheSeed()
    {
        var statsSa = PriceBook.Default.Files[0];
        var corrected = statsSa with { Products = [statsSa.Products[0]] };
        await Files().InsertOneAsync(PriceFileDocument.Of(corrected));

        var latest = await MongoPriceStore.Open(Url!, _database).LatestAsync(CancellationToken.None);

        Assert.That(latest[0].Products, Has.Count.EqualTo(1));
    }

    /// <summary>
    /// An upload saves a new month, replaces a stored one with its correction, and every month of a source comes back
    /// oldest first (F55).
    /// </summary>
    [Test]
    public async Task AnUpload_SavesAMonth_AndReplacesACorrectedOne()
    {
        var store = MongoPriceStore.Open(Url!, _database);
        var statsSa = PriceBook.Default.Files[0];
        var september = statsSa with { AsOf = new DateOnly(2026, 9, 1), Products = [statsSa.Products[0]] };
        var correctedAugust = statsSa with { Products = [statsSa.Products[1]] };

        await store.SaveAsync(september, CancellationToken.None);
        await store.SaveAsync(correctedAugust, CancellationToken.None);
        var months = await store.MonthsAsync(statsSa.Source, CancellationToken.None);

        Assert.That(store.CanSave, Is.True);
        Assert.That(months.Select(m => m.AsOf), Is.EqualTo(new[] { statsSa.AsOf, september.AsOf }));
        Assert.That(months[0].Products.Single().Product, Is.EqualTo(statsSa.Products[1].Product),
            "August is the corrected one, not the seed");
    }
}
