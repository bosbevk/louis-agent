using LouisAgent.Composition.Messages;
using System.Globalization;
using System.Text;
using ExcelDataReader;
using LouisAgent.Core.Messages;
using LouisAgent.Packs.ShopPrices.Messages;
using LouisAgent.Packs.ShopPrices.Prices.Regions;

namespace LouisAgent.Packs.ShopPrices.Prices.StatsSa.Sheets;

/// <summary>
/// Reads Stats SA's two average-price spreadsheets (P0141) into rows. Each sheet is wide: one row per product and pack
/// (and province), one column per month (<c>M201701</c> to the newest), so a release adds columns. The columns are
/// found by their header names, not their places: <c>H03</c> the code, <c>H04</c> the name, <c>H08</c> the pack size,
/// <c>H09</c> the province. A blank cell is no price; a file or a cell that can't be read is a problem, never a throw.
/// </summary>
/// <remarks>
/// A Table Data Gateway's reading half over a file (Fowler, PoEAA): one class knows the sheet's layout, and everything
/// after it works on <see cref="StatsSaRow"/>s. The cells are read by ExcelDataReader (MIT), already core's reader for
/// <c>ReadTable</c>; the month names are read with plain string methods (ADR 0057).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0057-regex-is-the-last-option.md">ADR 0057: a regular expression is the last option</seealso>
public static class StatsSaSheets
{
    private const string CodeColumn = "H03";
    private const string ProductColumn = "H04";
    private const string SizeColumn = "H08";
    private const string ProvinceColumn = "H09";

    // ExcelDataReader needs the legacy code pages even for .xlsx, which .NET only has once they are registered.
    static StatsSaSheets() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    /// <summary>Every price in the two files, and what couldn't be read. The files must be seekable.</summary>
    public static StatsSaRows Read(Stream allUrban, Stream provinces)
    {
        var rows = new List<StatsSaRow>();
        var problems = new List<string>();
        ReadFile(allUrban, StatsSaSheet.AllUrban, rows, problems);
        ReadFile(provinces, StatsSaSheet.Provinces, rows, problems);
        return new StatsSaRows(rows, problems);
    }

    /// <summary>Reads one file's first sheet; a file that isn't a spreadsheet is one problem.</summary>
    private static void ReadFile(Stream file, StatsSaSheet sheet, List<StatsSaRow> rows, List<string> problems)
    {
        try
        {
            using var reader = ExcelReaderFactory.CreateReader(file);
            ReadSheet(reader, sheet, rows, problems);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            problems.Add(MessageText.Format(ShopPriceImportMessage.SheetUnreadable,
                new { Sheet = MessageText.Template(sheet), Reason = ex.Message }));
        }
    }

    /// <summary>Finds the columns in the header row, then reads every row below it.</summary>
    private static void ReadSheet(IExcelDataReader reader, StatsSaSheet sheet, List<StatsSaRow> rows,
        List<string> problems)
    {
        if (!reader.Read()) return;
        var header = Enumerable.Range(0, reader.FieldCount).ToDictionary(i => i, i => Text(reader, i));
        var columns = header.Where(c => c.Value.Length > 0).GroupBy(c => c.Value)
            .ToDictionary(g => g.Key, g => g.First().Key, StringComparer.OrdinalIgnoreCase);
        var months = header.Select(c => (Index: c.Key, Month: MonthOf(c.Value)))
            .Where(c => c.Month is not null).Select(c => (c.Index, Month: c.Month!.Value)).ToList();

        string[] needed = sheet == StatsSaSheet.Provinces
            ? [CodeColumn, ProductColumn, SizeColumn, ProvinceColumn]
            : [CodeColumn, ProductColumn, SizeColumn];
        var missing = needed.Where(name => !columns.ContainsKey(name)).ToList();
        problems.AddRange(missing.Select(name => MessageText.Format(ShopPriceImportMessage.ColumnMissing,
            new { Sheet = MessageText.Template(sheet), Column = name })));
        if (missing.Count > 0) return;
        if (months.Count == 0)
        {
            problems.Add(MessageText.Format(ShopPriceImportMessage.NoMonths, new { Sheet = MessageText.Template(sheet) }));
            return;
        }

        var place = new SheetPlace(sheet, columns, months);
        for (int row = 2; reader.Read(); row++) ReadRow(reader, place, row, rows, problems);
    }

    /// <summary>One product's row: a row of ours for each month it has a price.</summary>
    private static void ReadRow(IExcelDataReader reader, SheetPlace place, int row, List<StatsSaRow> rows,
        List<string> problems)
    {
        string code = Text(reader, place.Columns[CodeColumn]);
        if (code.Length == 0) return;
        if (RegionOf(reader, place, row, problems) is not { } region) return;

        string product = Text(reader, place.Columns[ProductColumn]);
        string size = Text(reader, place.Columns[SizeColumn]);
        foreach (var (index, month) in place.Months)
        {
            string text = Text(reader, index);
            if (text.Length == 0) continue;
            if (PriceOf(reader.GetValue(index), text) is { } price)
            {
                rows.Add(new StatsSaRow(code, product, size, region, month, price));
                continue;
            }

            problems.Add(MessageText.Format(ShopPriceImportMessage.PriceUnreadable,
                new { Sheet = MessageText.Template(place.Sheet), Row = row, Month = $"{month:yyyy-MM}", Text = text }));
        }
    }

    /// <summary>The country for the all-urban file; the row's province for the provinces' file, or a problem.</summary>
    private static Region? RegionOf(IExcelDataReader reader, SheetPlace place, int row, List<string> problems)
    {
        if (place.Sheet == StatsSaSheet.AllUrban) return Region.National;

        string province = Text(reader, place.Columns[ProvinceColumn]);
        if (province.Length > 0 && RegionNames.TryParse(province, out var region)) return region;

        problems.Add(MessageText.Format(ShopPriceImportMessage.ProvinceUnknown,
            new { Sheet = MessageText.Template(place.Sheet), Row = row, Province = province }));
        return null;
    }

    /// <summary>"M202608" is August 2026; any other header is not a month.</summary>
    internal static DateOnly? MonthOf(string header)
    {
        bool shaped = header.Length == 7 && header[0] is 'M' or 'm';
        if (!shaped || !int.TryParse(header.AsSpan(1, 4), CultureInfo.InvariantCulture, out int year)) return null;
        if (!int.TryParse(header.AsSpan(5, 2), CultureInfo.InvariantCulture, out int month)) return null;
        return month is >= 1 and <= 12 ? new DateOnly(year, month, 1) : null;
    }

    /// <summary>A price as Stats SA writes it: a number cell, or text such as "27.86".</summary>
    private static decimal? PriceOf(object value, string text) => value switch
    {
        double number => PlainNumber.Of((decimal)number),
        _ => decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsed)
            ? PlainNumber.Of(parsed)
            : null,
    };

    /// <summary>A cell as trimmed text; an empty cell is "".</summary>
    private static string Text(IExcelDataReader reader, int index) =>
        Convert.ToString(reader.GetValue(index), CultureInfo.InvariantCulture)?.Trim() ?? "";

    /// <summary>Where one sheet's columns are: by header name, and the month columns in their order.</summary>
    private sealed record SheetPlace(
        StatsSaSheet Sheet,
        IReadOnlyDictionary<string, int> Columns,
        IReadOnlyList<(int Index, DateOnly Month)> Months);
}
