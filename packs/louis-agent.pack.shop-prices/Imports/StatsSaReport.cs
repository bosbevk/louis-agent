using LouisAgent.Composition.Messages;
using LouisAgent.Composition.Packs.Imports;
using System.Globalization;
using LouisAgent.Core.Messages;
using LouisAgent.Packs.ShopPrices.Messages;

namespace LouisAgent.Packs.ShopPrices.Imports;

/// <summary>
/// Writes an upload as the report a person reads before saving: the months in the files, the new ones, each corrected
/// month with its first changes, the unchanged ones, the products in the newest month and those with no names yet.
/// </summary>
public static class StatsSaReport
{
    /// <summary>How many of a corrected month's changes the preview shows.</summary>
    private const int Examples = 3;

    /// <summary>How many months are named one by one before a list becomes "first to last".</summary>
    private const int Named = 6;

    /// <summary>The report of the upload, saved or not.</summary>
    public static ImportReport Of(StatsSaUpload upload, bool saved)
    {
        if (upload.Months.Count == 0) return new ImportReport(null, 0, false, [], upload.Problems);

        var months = upload.Months.Select(m => m.AsOf).ToList();
        string period = Period(months);
        var newest = upload.Months[^1];
        List<ImportFact> facts =
        [
            Fact(ShopPriceImportMessage.MonthsLabel, ShopPriceImportMessage.MonthsValue, new { months.Count, Period = period }),
            new(Text(ShopPriceImportMessage.NewMonthsLabel), List(upload.New.Select(m => m.AsOf).ToList())),
            .. upload.Corrected.Select(Corrected),
            new(Text(ShopPriceImportMessage.UnchangedMonthsLabel), upload.Unchanged.ToString(CultureInfo.InvariantCulture)),
            new(MessageText.Format(ShopPriceImportMessage.ProductsLabel, new { Month = Month(newest.AsOf) }),
                newest.Products.Count.ToString(CultureInfo.InvariantCulture)),
            new(Text(ShopPriceImportMessage.UnnamedLabel),
                upload.Unnamed.Count == 0 ? Text(ShopPriceImportMessage.None) : string.Join(", ", upload.Unnamed)),
        ];
        return new ImportReport(period, upload.ToSave.Count, saved, facts, upload.Problems);
    }

    /// <summary>"March 2026": "12 changes: …" with the first few.</summary>
    private static ImportFact Corrected(CorrectedMonth month)
    {
        string examples = string.Join("; ", month.Changes.Take(Examples)) + (month.Changes.Count > Examples ? "; …" : "");
        return Fact(ShopPriceImportMessage.CorrectedLabel, ShopPriceImportMessage.CorrectedValue,
            new { Month = Month(month.File.AsOf), month.Changes.Count, Examples = examples }, new { Month = Month(month.File.AsOf) });
    }

    /// <summary>A count, and the months by name when there are few, else from the first to the last.</summary>
    private static string List(IReadOnlyList<DateOnly> months)
    {
        string count = months.Count.ToString(CultureInfo.InvariantCulture);
        if (months.Count == 0) return count;
        string named = months.Count <= Named ? string.Join(", ", months.Select(Month)) : Period(months);
        return $"{count} ({named})";
    }

    /// <summary>"January 2017 to August 2026", or the one month.</summary>
    private static string Period(IReadOnlyList<DateOnly> months) =>
        months.Count == 1
            ? Month(months[0])
            : MessageText.Format(ShopPriceImportMessage.Period, new { First = Month(months.Min()), Last = Month(months.Max()) });

    /// <summary>"August 2026".</summary>
    private static string Month(DateOnly month) => month.ToString("MMMM yyyy", CultureInfo.InvariantCulture);

    /// <summary>A message's text with no holes.</summary>
    private static string Text(ShopPriceImportMessage message) => MessageText.Format(message);

    /// <summary>A fact whose label and value are both templates.</summary>
    private static ImportFact Fact(ShopPriceImportMessage label, ShopPriceImportMessage value, object values,
        object? labelValues = null) =>
        new(MessageText.Format(label, labelValues ?? values), MessageText.Format(value, values));
}
