namespace LouisAgent.Core.Tools.Documents;

using System.Globalization;
using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ExcelDataReader;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

/// <summary>
/// The text of a PDF, a Word document or a spreadsheet (F22), for <c>ReadDocument</c> (files) and <c>FetchUrl</c> (web).
/// The reading is the libraries' (Louis chose them: PdfPig, DocumentFormat.OpenXml, ExcelDataReader); this only joins
/// their output into text with page and sheet markers. No OCR: a scanned PDF has no text.
/// </summary>
internal static class DocumentText
{
    /// <summary>The extensions it reads, lowercase.</summary>
    public static readonly IReadOnlyList<string> Extensions = [".pdf", ".docx", ".xlsx", ".xls", ".csv"];

    // ExcelDataReader reads .xls and CSV in their own code pages, which .NET only has once registered.
    static DocumentText() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    public static bool CanRead(string extension) => Extensions.Contains(extension.ToLowerInvariant());

    /// <summary>The text of <paramref name="stream"/>, read as <paramref name="extension"/>; throws when the file is damaged.</summary>
    public static string Read(Stream stream, string extension)
    {
        // The libraries seek; a network or file stream may not.
        using var seekable = new MemoryStream();
        stream.CopyTo(seekable);
        seekable.Position = 0;
        return extension.ToLowerInvariant() switch
        {
            ".pdf" => Pdf(seekable),
            ".docx" => Word(seekable),
            ".xlsx" or ".xls" => Sheets(ExcelReaderFactory.CreateReader(seekable), named: true),
            ".csv" => Sheets(ExcelReaderFactory.CreateCsvReader(seekable), named: false),
            _ => throw new NotSupportedException(extension),
        };
    }

    /// <summary>The extensions <see cref="Table"/> reads.</summary>
    public static readonly IReadOnlyList<string> TableExtensions = [".xlsx", ".xls", ".csv"];

    /// <summary>
    /// The rows of one sheet of a spreadsheet or CSV: <paramref name="sheet"/>, or the first when it's empty; every sheet's
    /// name too. <c>Rows</c> is null when there's no sheet of that name.
    /// </summary>
    public static (IReadOnlyList<string> Sheets, string Sheet, List<string[]>? Rows) Table(Stream stream, string extension, string? sheet)
    {
        using var seekable = new MemoryStream();
        stream.CopyTo(seekable);
        seekable.Position = 0;
        bool csv = extension.Equals(".csv", StringComparison.OrdinalIgnoreCase);
        using var reader = csv ? ExcelReaderFactory.CreateCsvReader(seekable) : ExcelReaderFactory.CreateReader(seekable);

        var sheets = new List<string>();
        List<string[]>? chosen = null;
        string chosenName = "";
        do
        {
            string name = csv ? "csv" : reader.Name;
            sheets.Add(name);
            bool wanted = chosen is null && (string.IsNullOrWhiteSpace(sheet) || name.Equals(sheet.Trim(), StringComparison.OrdinalIgnoreCase));
            if (!wanted) continue;
            chosenName = name;
            chosen = [];
            while (reader.Read())
                chosen.Add(Enumerable.Range(0, reader.FieldCount)
                    .Select(i => Convert.ToString(reader.GetValue(i), CultureInfo.InvariantCulture)?.Trim() ?? "").ToArray());
            // Trailing empty rows and columns are spreadsheet padding.
            while (chosen.Count > 0 && chosen[^1].All(c => c.Length == 0)) chosen.RemoveAt(chosen.Count - 1);
        } while (reader.NextResult());
        return (sheets, chosenName, chosen);
    }

    private static string Pdf(Stream stream)
    {
        using var document = PdfDocument.Open(stream);
        var text = new StringBuilder();
        foreach (var page in document.GetPages())
        {
            string content = ContentOrderTextExtractor.GetText(page).Trim();
            if (content.Length == 0) continue;
            text.AppendLine($"--- Page {page.Number} ---").AppendLine(content);
        }
        return text.ToString().Trim();
    }

    private static string Word(Stream stream)
    {
        using var document = WordprocessingDocument.Open(stream, isEditable: false);
        var text = new StringBuilder();
        foreach (var element in document.MainDocumentPart?.Document?.Body?.Elements() ?? [])
        {
            if (element is Paragraph paragraph) text.AppendLine(paragraph.InnerText);
            else if (element is Table table)
                foreach (var row in table.Elements<TableRow>())
                    text.AppendLine(string.Join('\t', row.Elements<TableCell>().Select(cell => cell.InnerText.Trim())));
        }
        return text.ToString().Trim();
    }

    private static string Sheets(IExcelDataReader reader, bool named)
    {
        using (reader)
        {
            var text = new StringBuilder();
            do
            {
                if (named) text.AppendLine($"--- Sheet {reader.Name} ---");
                while (reader.Read())
                {
                    var cells = Enumerable.Range(0, reader.FieldCount)
                        .Select(i => Convert.ToString(reader.GetValue(i), CultureInfo.InvariantCulture) ?? "");
                    string line = string.Join('\t', cells).TrimEnd();
                    if (line.Length > 0) text.AppendLine(line);
                }
            } while (reader.NextResult());
            return text.ToString().Trim();
        }
    }
}
