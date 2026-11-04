using System.Text.Json;
using LouisAgent.Choices;
using LouisAgent.Core.Agent.Catalog;
using LouisAgent.Core.Messages;
using LouisAgent.Packs.ShopPrices.Messages;
using LouisAgent.Packs.ShopPrices.Prices;
using LouisAgent.Packs.ShopPrices.Prices.Store;

namespace LouisAgent.Packs.ShopPrices.Feeds;

/// <summary>
/// The pack's price tables as a feed anyone can read (ADR 0070): the same tables the tool estimates from, read through
/// the same cache, under a notice that says they are averages and whose they are.
/// </summary>
/// <remarks>
/// A plug-in of core's <see cref="IDataFeed"/> (Plugin, Fowler): the API lists and serves it without knowing this
/// pack. It shares the tool's <see cref="PriceBookCache"/>, so a busy feed reads the store at most once an hour too,
/// and a store that is down answers with the shipped tables, as the tool does.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0070-packs-publish-data-feeds.md">ADR 0070: packs publish data feeds</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0069-shop-prices-are-stats-sa-estimates.md">ADR 0069: shop prices are Stats SA's averages</seealso>
public sealed class ShopPricesFeed(PriceBookCache books) : IDataFeed
{
    /// <inheritdoc />
    public string Name => ShopPricesToolset.ShopPrices.Wire();

    /// <inheritdoc />
    public string Description => MessageText.Format(ShopPriceFeedMessage.Description);

    /// <inheritdoc />
    public async Task<string> ReadJsonAsync(CancellationToken cancellationToken)
    {
        var book = await books.GetAsync(cancellationToken);
        var feed = new PriceFeed(MessageText.Format(ShopPriceFeedMessage.Notice), book.Files);
        return JsonSerializer.Serialize(feed, PriceBook.Json);
    }
}
