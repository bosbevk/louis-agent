using System.Text.Json;
using LouisAgent.Choices;
using LouisAgent.Packs.ShopPrices.Feeds;
using LouisAgent.Packs.ShopPrices.Prices;
using LouisAgent.Packs.ShopPrices.Prices.Files;
using LouisAgent.Packs.ShopPrices.Prices.Store;

namespace LouisAgent.Packs.ShopPrices.Tests.Feeds;

/// <summary>
/// The pack publishes its price tables as one feed (ADR 0070): the tables the tool prices from, in the shipped files'
/// shape, under a notice that says they are averages and whose they are.
/// </summary>
public class ShopPricesFeedTests
{
    /// <summary>The pack names one feed, <c>shop-prices</c>, beside its one toolset.</summary>
    [Test]
    public void ThePack_PublishesOneFeed_NamedLikeIt()
    {
        var pack = new ShopPricesPack(new ShopPricesOptions());

        Assert.That(pack.Feeds.Select(f => f.Name), Is.EqualTo(new[] { ShopPricesToolset.ShopPrices.Wire() }));
        Assert.That(pack.Feeds[0].Description, Does.Contain("Stats SA"));
    }

    /// <summary>
    /// The feed is the tables the tool prices from, Stats SA's first, in the shipped files' shape (units by their wire
    /// names, the date as a day), under the notice.
    /// </summary>
    [Test]
    public async Task TheFeed_IsTheShippedTables_UnderTheNotice()
    {
        var feed = new ShopPricesFeed(new PriceBookCache(new ShippedPrices(), TimeProvider.System));

        string json = await feed.ReadJsonAsync(CancellationToken.None);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var files = root.GetProperty("tables");

        Assert.That(root.GetProperty("notice").GetString(),
            Does.StartWith("Average prices over many shops, not a shop's price.").And.Contain("Statistics South Africa"));
        Assert.That(files.GetArrayLength(), Is.EqualTo(PriceBook.Default.Files.Count));
        Assert.That(files[0].GetProperty("source").GetString(), Does.StartWith("Stats SA"));
        Assert.That(files[0].GetProperty("as_of").GetString(), Is.EqualTo("2026-08-01"));
        var pack = files[0].GetProperty("products")[0].GetProperty("packs")[0];
        Assert.That(pack.GetProperty("unit").GetString(), Is.EqualTo("g"));
        Assert.That(pack.GetProperty("prices").GetProperty("western-cape").GetDecimal(), Is.GreaterThan(0));
    }

    /// <summary>The feed reads what the store holds now: a newer month in the store is the month it shows.</summary>
    [Test]
    public async Task TheFeed_ReadsTheStore()
    {
        var september = new PriceFile("Stats SA, CPI average prices (P0141)", new DateOnly(2026, 9, 1), []);
        var feed = new ShopPricesFeed(new PriceBookCache(new OneMonth(september), TimeProvider.System));

        using var document = JsonDocument.Parse(await feed.ReadJsonAsync(CancellationToken.None));

        Assert.That(document.RootElement.GetProperty("tables").EnumerateArray().Select(f => f.GetProperty("as_of").GetString()),
            Is.EqualTo(new[] { "2026-09-01" }));
    }

    /// <summary>A store that holds one table.</summary>
    private sealed class OneMonth(PriceFile file) : IPriceStore
    {
        /// <inheritdoc />
        public Task<IReadOnlyList<PriceFile>> LatestAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<PriceFile>>([file]);

        /// <inheritdoc />
        public bool CanSave => false;

        /// <inheritdoc />
        public Task<IReadOnlyList<PriceFile>> MonthsAsync(string source, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<PriceFile>>([]);

        /// <inheritdoc />
        public Task SaveAsync(PriceFile file, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
