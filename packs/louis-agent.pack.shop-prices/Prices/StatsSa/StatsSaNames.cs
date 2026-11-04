using System.Text.Json;
using LouisAgent.Packs.ShopPrices.Prices.StatsSa.Sheets;

namespace LouisAgent.Packs.ShopPrices.Prices.StatsSa;

/// <summary>
/// The names we give Stats SA's products, by Stats SA's code, from <c>Prices/Data/statssa-names.json</c>: the name we
/// show and the names a shopping list may use. Kept by the code, so a release that rewords a product keeps its names;
/// a product the file lacks is named by its own tidied name until a person adds it.
/// </summary>
/// <remarks>
/// Content, not code (ADR 0011): a person reviews the file in git, like <c>own-prices.json</c>. A class, not a
/// record: it owns the lookup by code (ADR 0040).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0011-prompts-are-content.md">ADR 0011: content, not code</seealso>
public sealed class StatsSaNames
{
    /// <summary>The names file the pack ships, read once for the process.</summary>
    private static readonly Lazy<StatsSaNames> Embedded = new(() =>
    {
        using var stream = typeof(StatsSaNames).Assembly.GetManifestResourceStream("ShopPrices.statssa-names.json")
                           ?? throw new InvalidOperationException("The pack ships no statssa-names.json.");
        return new StatsSaNames(JsonSerializer.Deserialize<List<StatsSaName>>(stream) ?? []);
    });

    private readonly Dictionary<string, StatsSaName> _byCode;

    /// <summary>The names in these lines; a code listed twice keeps its first line.</summary>
    public StatsSaNames(IReadOnlyList<StatsSaName> names) =>
        _byCode = names.DistinctBy(name => name.Code).ToDictionary(name => name.Code, StringComparer.Ordinal);

    /// <summary>The names file the pack ships.</summary>
    public static StatsSaNames Default => Embedded.Value;

    /// <summary>The product's names: the file's, or its own tidied name when the file doesn't list its code.</summary>
    public StatsSaName For(StatsSaRow row)
    {
        if (_byCode.TryGetValue(row.Code, out var named)) return named;
        string product = StatsSaProduct.Tidy(row.Product);
        return new StatsSaName(row.Code, product, [product.ToLowerInvariant()]);
    }

    /// <summary>The tidied names of the products in these rows that the file doesn't list, once each.</summary>
    public IReadOnlyList<string> Unnamed(IEnumerable<StatsSaRow> rows) =>
    [
        .. rows.Where(row => !_byCode.ContainsKey(row.Code)).DistinctBy(row => row.Code)
            .Select(row => StatsSaProduct.Tidy(row.Product))
    ];
}
