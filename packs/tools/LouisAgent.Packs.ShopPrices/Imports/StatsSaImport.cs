using LouisAgent.Composition.Messages;
using LouisAgent.Composition.Packs.Imports;
using LouisAgent.Choices;
using LouisAgent.Packs.ShopPrices.Messages;
using LouisAgent.Packs.ShopPrices.Prices.Files;
using LouisAgent.Packs.ShopPrices.Prices.StatsSa;
using LouisAgent.Packs.ShopPrices.Prices.StatsSa.Sheets;
using LouisAgent.Packs.ShopPrices.Prices.Store;

namespace LouisAgent.Packs.ShopPrices.Imports;

/// <summary>
/// Stats SA's two spreadsheets uploaded from the admin portal (F55): the preview reads every month in them and sets
/// each beside the store (new, corrected or unchanged); Save writes the new and the corrected months, then drops the
/// cached book so the next estimate prices from them. Stats SA publishes when it publishes, not every month, so this
/// is how a release arrives, with no code change and no deploy.
/// </summary>
/// <remarks>
/// A plug-in of core's <see cref="IDataImport"/> (Plugin, Fowler): the API lists and calls it without knowing this
/// pack (ADR 0027). Preview and Save take the same files, so nothing is held between them; a person checks the
/// preview, and Save writes what it listed (Louis's pick F1, 2026-11-04).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/packs/docs/adr/0069-shop-prices-are-stats-sa-estimates.md">ADR 0069: shop prices are Stats SA's averages</seealso>
public sealed class StatsSaImport(IPriceStore store, PriceBookCache books, StatsSaNames names) : IDataImport
{
    /// <inheritdoc />
    public string Name => ShopPricesToolset.ShopPrices.Wire();

    /// <inheritdoc />
    public string Description => MessageText.Format(ShopPriceImportMessage.Description);

    /// <inheritdoc />
    public IReadOnlyList<ImportSlot> Files { get; } =
    [
        new(StatsSaSheet.AllUrban.Wire(), MessageText.Format(ShopPriceImportMessage.AllUrbanSlot)),
        new(StatsSaSheet.Provinces.Wire(), MessageText.Format(ShopPriceImportMessage.ProvincesSlot)),
    ];

    /// <inheritdoc />
    public async Task<ImportReport> PreviewAsync(IReadOnlyList<ImportFile> files, CancellationToken cancellationToken) =>
        StatsSaReport.Of(await UploadAsync(files, cancellationToken), saved: false);

    /// <inheritdoc />
    public async Task<ImportReport> SaveAsync(IReadOnlyList<ImportFile> files, CancellationToken cancellationToken)
    {
        var upload = await UploadAsync(files, cancellationToken);
        if (upload.Problems.Count > 0 || upload.ToSave.Count == 0) return StatsSaReport.Of(upload, saved: false);

        foreach (var month in upload.ToSave) await store.SaveAsync(month, cancellationToken);
        books.Forget();
        return StatsSaReport.Of(upload, saved: true);
    }

    /// <summary>Reads the two files and sets every month in them beside the store.</summary>
    private async Task<StatsSaUpload> UploadAsync(IReadOnlyList<ImportFile> files, CancellationToken cancellationToken)
    {
        var allUrban = files.FirstOrDefault(f => f.Slot == StatsSaSheet.AllUrban.Wire());
        var provinces = files.FirstOrDefault(f => f.Slot == StatsSaSheet.Provinces.Wire());
        if (allUrban is null || provinces is null) return StatsSaUpload.Failed([.. Missing(allUrban, provinces)]);

        var read = StatsSaSheets.Read(new MemoryStream(allUrban.Content), new MemoryStream(provinces.Content));
        if (read.Problems.Count > 0) return StatsSaUpload.Failed(read.Problems);
        var months = StatsSaPrices.ToFiles(read.Rows, names);
        if (months.Count == 0) return StatsSaUpload.Failed([MessageText.Format(ShopPriceImportMessage.NoPrices)]);

        IReadOnlyList<PriceFile> stored;
        try
        {
            stored = await store.MonthsAsync(StatsSaPrices.Source, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return StatsSaUpload.Failed(
                [MessageText.Format(ShopPriceImportMessage.StoreUnavailable, new { Reason = ex.Message })]);
        }

        return Compare(months, stored, names.Unnamed(read.Rows));
    }

    /// <summary>Each month in the files beside the stored one: new, corrected (with its changes) or unchanged.</summary>
    private StatsSaUpload Compare(IReadOnlyList<PriceFile> months, IReadOnlyList<PriceFile> stored,
        IReadOnlyList<string> unnamed)
    {
        var byMonth = stored.ToDictionary(file => file.AsOf);
        var fresh = months.Where(m => !byMonth.ContainsKey(m.AsOf)).ToList();
        var changed = months.Where(m => byMonth.ContainsKey(m.AsOf))
            .Select(m => new CorrectedMonth(m, MonthChanges.Between(byMonth[m.AsOf], m))).ToList();
        var corrected = changed.Where(c => c.Changes.Count > 0).ToList();
        List<string> problems = store.CanSave ? [] : [MessageText.Format(ShopPriceImportMessage.StoreCantSave)];
        return new StatsSaUpload(months, fresh, corrected, changed.Count - corrected.Count, unnamed, problems);
    }

    /// <summary>A problem for each file not given.</summary>
    private static IEnumerable<string> Missing(ImportFile? allUrban, ImportFile? provinces)
    {
        if (allUrban is null)
            yield return MessageText.Format(ShopPriceImportMessage.FileMissing, new { Sheet = MessageText.Template(StatsSaSheet.AllUrban) });
        if (provinces is null)
            yield return MessageText.Format(ShopPriceImportMessage.FileMissing, new { Sheet = MessageText.Template(StatsSaSheet.Provinces) });
    }
}
