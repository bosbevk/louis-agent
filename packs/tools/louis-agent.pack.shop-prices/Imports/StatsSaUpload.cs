using LouisAgent.Packs.ShopPrices.Prices.Files;

namespace LouisAgent.Packs.ShopPrices.Imports;

/// <summary>
/// What an upload of Stats SA's spreadsheets holds, set beside the store: every month in it, the months the store
/// doesn't have, the stored months it corrects (with what changed), how many it holds unchanged, the products the names
/// file lacks, and the problems that keep it from being saved.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values</seealso>
public sealed record StatsSaUpload(
    IReadOnlyList<PriceFile> Months,
    IReadOnlyList<PriceFile> New,
    IReadOnlyList<CorrectedMonth> Corrected,
    int Unchanged,
    IReadOnlyList<string> Unnamed,
    IReadOnlyList<string> Problems)
{
    /// <summary>An upload that can't go further: no months, only its problems.</summary>
    public static StatsSaUpload Failed(IReadOnlyList<string> problems) => new([], [], [], 0, [], problems);

    /// <summary>The months Save writes: the new ones and the corrected ones.</summary>
    public IReadOnlyList<PriceFile> ToSave => [.. New, .. Corrected.Select(c => c.File)];
}

/// <summary>A stored month that the upload changes, and each change as a line for the preview.</summary>
public sealed record CorrectedMonth(PriceFile File, IReadOnlyList<string> Changes);
