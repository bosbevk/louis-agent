using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace LouisAgent.Core.Tests.Tools.Documents;

/// <summary>
/// Small documents built in the test with the libraries that read them (F22), so the repo holds no binary fixtures.
/// </summary>
internal static class DocumentFiles
{
    /// <summary>A PDF with one line of text per page; a page with no lines has no text (as a scanned page would).</summary>
    public static byte[] Pdf(params string[][] pages)
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        foreach (string[] lines in pages)
        {
            var page = builder.AddPage(PageSize.A4);
            for (int i = 0; i < lines.Length; i++) page.AddText(lines[i], 12, new PdfPoint(40, 780 - i * 20), font);
        }
        return builder.Build();
    }

    /// <summary>A Word document: the paragraphs, then a table of the rows.</summary>
    public static void Docx(string path, string[] paragraphs, string[][] rows)
    {
        using var document = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
        var body = new W.Body();
        foreach (string text in paragraphs) body.Append(new W.Paragraph(new W.Run(new W.Text(text))));
        var table = new W.Table();
        foreach (string[] row in rows)
            table.Append(new W.TableRow(row.Select(cell => new W.TableCell(new W.Paragraph(new W.Run(new W.Text(cell)))))));
        body.Append(table);
        document.AddMainDocumentPart().Document = new W.Document(body);
    }

    /// <summary>A workbook with one sheet of the rows; cells that parse as numbers are numbers.</summary>
    public static void Xlsx(string path, string sheetName, string[][] rows)
    {
        using var document = SpreadsheetDocument.Create(path, SpreadsheetDocumentType.Workbook);
        var workbook = document.AddWorkbookPart();
        workbook.Workbook = new Workbook();
        var worksheet = workbook.AddNewPart<WorksheetPart>();
        var data = new SheetData();
        for (int r = 0; r < rows.Length; r++)
        {
            var row = new Row { RowIndex = (uint)(r + 1) };
            for (int c = 0; c < rows[r].Length; c++)
            {
                string value = rows[r][c];
                string reference = $"{(char)('A' + c)}{r + 1}";
                row.Append(double.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, out _)
                    ? new Cell { CellReference = reference, DataType = CellValues.Number, CellValue = new CellValue(value) }
                    : new Cell { CellReference = reference, DataType = CellValues.InlineString, InlineString = new InlineString(new Text(value)) });
            }
            data.Append(row);
        }
        worksheet.Worksheet = new Worksheet(data);
        workbook.Workbook.AppendChild(new Sheets()).Append(new Sheet { Id = workbook.GetIdOfPart(worksheet), SheetId = 1, Name = sheetName });
    }
}
