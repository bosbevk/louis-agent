using LouisAgent.Packs.ShopPrices.Prices;
using LouisAgent.Packs.ShopPrices.Prices.Files;
using LouisAgent.Packs.ShopPrices.Prices.Store;
using Microsoft.Extensions.Time.Testing;

namespace LouisAgent.Packs.ShopPrices.Tests.Prices.Store;

/// <summary>
/// The cache reads the store at most once an hour, and when the store fails or holds nothing, the shipped tables
/// stand in, so a price is always there.
/// </summary>
public class PriceBookCacheTests
{
    /// <summary>A table of one product, so a book read from the store is told apart from the shipped one.</summary>
    private static readonly PriceFile StoreTable = new("a test store", new DateOnly(2026, 9, 1),
    [
        new PriceProduct("Rice", ["rice"],
            [new PricePack("1 Kilogram", 1000, PackUnit.Gram, new Dictionary<string, decimal> { ["national"] = 30m })])
    ]);

    /// <summary>A store that counts its reads and answers with what the test gives it.</summary>
    private sealed class CountingStore(Func<IReadOnlyList<PriceFile>> answer) : IPriceStore
    {
        /// <summary>How many times it was read.</summary>
        public int Reads { get; private set; }

        /// <inheritdoc />
        public Task<IReadOnlyList<PriceFile>> LatestAsync(CancellationToken cancellationToken)
        {
            Reads++;
            return Task.FromResult(answer());
        }
    }

    /// <summary>Two calls within the hour read the store once; after the hour, it is read again.</summary>
    [Test]
    public async Task TheStore_IsReadOnceAnHour()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 11, 4, 12, 0, 0, TimeSpan.Zero));
        var store = new CountingStore(() => [StoreTable]);
        var cache = new PriceBookCache(store, clock);

        var first = await cache.GetAsync(CancellationToken.None);
        clock.Advance(TimeSpan.FromMinutes(59));
        var second = await cache.GetAsync(CancellationToken.None);

        Assert.That(store.Reads, Is.EqualTo(1));
        Assert.That(second, Is.SameAs(first));
        Assert.That(first.Files.Single().Source, Is.EqualTo("a test store"), "the store's tables, not the shipped ones");

        clock.Advance(TimeSpan.FromMinutes(1));
        await cache.GetAsync(CancellationToken.None);
        Assert.That(store.Reads, Is.EqualTo(2));
    }

    /// <summary>A store that throws gives the shipped tables, and is tried again after the hour.</summary>
    [Test]
    public async Task AStoreThatFails_GivesTheShippedTables_UntilTheNextRead()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 11, 4, 12, 0, 0, TimeSpan.Zero));
        var failing = true;
        var store = new CountingStore(() => failing ? throw new TimeoutException("no server") : [StoreTable]);
        var cache = new PriceBookCache(store, clock);

        Assert.That(await cache.GetAsync(CancellationToken.None), Is.SameAs(PriceBook.Default));

        failing = false;
        clock.Advance(PriceBookCache.Fresh);
        Assert.That((await cache.GetAsync(CancellationToken.None)).Files.Single().Source, Is.EqualTo("a test store"));
    }

    /// <summary>A store with no tables gives the shipped ones, never an empty book.</summary>
    [Test]
    public async Task AnEmptyStore_GivesTheShippedTables() =>
        Assert.That(await new PriceBookCache(new CountingStore(() => []), TimeProvider.System)
            .GetAsync(CancellationToken.None), Is.SameAs(PriceBook.Default));

    /// <summary>Without a store, the pack's own tables are the book.</summary>
    [Test]
    public async Task TheShippedStore_HoldsThePacksTables() =>
        Assert.That(await new ShippedPrices().LatestAsync(CancellationToken.None),
            Is.SameAs(PriceBook.Default.Files));
}
