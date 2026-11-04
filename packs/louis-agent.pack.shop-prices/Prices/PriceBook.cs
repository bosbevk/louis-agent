using System.Text.Json;
using System.Text.Json.Serialization;
using LouisAgent.Packs.ShopPrices.Prices.Files;

namespace LouisAgent.Packs.ShopPrices.Prices;

/// <summary>
/// The prices we estimate from: Stats SA's average prices for one month, and our own rows for what Stats SA doesn't
/// price. It finds the product a shopping-list line is about by its words: the name with the most words that all
/// appear in the line wins, so "tomato sauce" beats "tomato", and on a tie Stats SA's row comes before ours.
/// </summary>
/// <remarks>
/// The tables are content embedded in the pack (ADR 0011), read once through records (ADR 0041), so a month's new
/// prices are a data change with no code. A class, not a record: it owns the loaded tables and the word index.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0011-prompts-are-content.md">ADR 0011: content, not code</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0069-shop-prices-are-stats-sa-estimates.md">ADR 0069: shop prices are Stats SA's averages</seealso>
public sealed class PriceBook
{
    /// <summary>The one options object for the price files, read and written (the feed): units by their wire names (ADR 0041).</summary>
    internal static readonly JsonSerializerOptions Json = new() { Converters = { new JsonStringEnumConverter() } };

    /// <summary>The pack's own tables, Stats SA's first, read once for the process.</summary>
    private static readonly Lazy<PriceBook> Embedded = new(() =>
        new PriceBook([Read("statssa-average-prices"), Read("own-prices")]));

    private readonly List<(IReadOnlyList<string> Words, PriceMatch Match)> _names;

    /// <summary>A table over these files, the first file's rows winning a tie.</summary>
    public PriceBook(IReadOnlyList<PriceFile> files)
    {
        Files = files;
        _names =
        [
            .. files.SelectMany(file => file.Products.SelectMany(product =>
                product.Names.Select(name => (ProductWords.Of(name), new PriceMatch(product, file)))))
        ];
    }

    /// <summary>The tables the pack ships: Stats SA's month and our own rows.</summary>
    public static PriceBook Default => Embedded.Value;

    /// <summary>The tables, in the order a tie is settled.</summary>
    public IReadOnlyList<PriceFile> Files { get; }

    /// <summary>The product a line is about, or nothing when no name fits it.</summary>
    public PriceMatch? Find(IReadOnlyList<string> lineWords)
    {
        PriceMatch? best = null;
        var bestCount = 0;
        foreach (var (words, match) in _names)
        {
            // Every word of the name must be in the line; a longer name that fits is the more exact one.
            if (words.Count <= bestCount || !words.All(lineWords.Contains)) continue;
            best = match;
            bestCount = words.Count;
        }

        return best;
    }

    /// <summary>One of the pack's embedded tables, by its file name.</summary>
    private static PriceFile Read(string name)
    {
        using var stream = typeof(PriceBook).Assembly.GetManifestResourceStream($"ShopPrices.{name}.json")
                           ?? throw new InvalidOperationException($"The price table {name}.json is not embedded.");
        return JsonSerializer.Deserialize<PriceFile>(stream, Json)
               ?? throw new InvalidOperationException($"The price table {name}.json is empty.");
    }
}
