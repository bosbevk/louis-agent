using LouisAgent.Composition.Messages;
using System.ComponentModel;
using LouisAgent.Core.Messages;
using LouisAgent.Choices;
using LouisAgent.Packs.ShopPrices.Estimates;
using LouisAgent.Packs.ShopPrices.Messages;
using LouisAgent.Packs.ShopPrices.Prices.Store;
using LouisAgent.Packs.ShopPrices.Prices.Regions;

namespace LouisAgent.Packs.ShopPrices.Tools;

/// <summary>
/// The shop-prices toolset: what a shopping list costs, estimated from Stats SA's average prices in the user's
/// province. Code matches each line to a product, scales the pack, adds the total and shares it per person, so the
/// numbers are the same every run and never the model's own.
/// </summary>
/// <remarks>
/// It reads no shop's site: the prices are Stats SA's tables (ADR 0069), from the price store at most an hour ago, or
/// the ones the pack ships when the store is unset or doesn't answer, so a price is always there. It only reads, so it
/// is safe to run beside any other tool. It takes plain data and returns text, so
/// the pack converts to an MCP server unchanged (ADR 0028).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0028-packs-stay-convertible-to-mcp.md">ADR 0028: packs stay convertible to MCP</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/packs/docs/adr/0069-shop-prices-are-stats-sa-estimates.md">ADR 0069: shop prices are Stats SA's averages</seealso>
public sealed class ShopPriceTools(PriceBookCache books)
{
    private const int MaxItems = 60;

    /// <summary>Estimates a shopping list's cost, line by line, with the total and the price per person.</summary>
    [Description(
        "Estimates what a shopping list costs from Stats SA's average prices (the user's province first, else the " +
        "national average): each line priced from the nearest pack Stats SA prices, scaled to the line's size, then the " +
        "total and the price per person, worked out in code. Averages over many shops, never one shop's price. Lines it " +
        "can't price come back as ?, never R0.")]
    public async Task<string> EstimatePrices(
        [Description(
            "The shopping list, one line per item, in English: what to buy and its pack, 'beef mince 500 g', " +
            "'eggs 18', 'rice 1 kg', 'tinned tomatoes 410 g x 2' (x 2 for two packs). Leave out what isn't bought.")]
        string[] items,
        [Description("The user's province, 'Western Cape' or 'Gauteng'; unset uses the national average")]
        string? province = null,
        [Description("How many people the list feeds, for the price per person; unset gives none")]
        int? serves = null,
        CancellationToken cancellationToken = default)
    {
        var lines = (items ?? []).Where(line => !string.IsNullOrWhiteSpace(line)).ToList();
        if (lines.Count == 0) return MessageText.Format(ShopPriceToolMessage.NoItems);
        if (lines.Count > MaxItems)
            return MessageText.Format(ShopPriceToolMessage.TooManyItems, new { Max = MaxItems, lines.Count });

        // A province we don't know is said, and the country's averages answer instead of nothing.
        string? unknown = RegionNames.TryParse(province, out var region)
            ? null
            : MessageText.Format(ShopPriceToolMessage.ProvinceUnknown,
                new { Province = province!.Trim(), Provinces = string.Join(", ", SettingChoice.Names<Region>()) });

        var book = await books.GetAsync(cancellationToken);
        return EstimatePrinter.Print(BasketEstimate.Of(lines, book, region, serves), book, unknown);
    }
}
