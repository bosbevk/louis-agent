namespace LouisAgent.Core.Tools.Documents;

using LouisAgent.Core.Guardrails.Workspace;
using System.ComponentModel;
using System.Text;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools.Documents.Messages;

/// <summary>Documents and spreadsheets in the workspace (the files toolset, S: one of its five classes; F22): their text in parts, a sheet as a table.</summary>
public sealed class DocumentTools
{
    private const int MaxDocumentBytes = 10_000_000;

    private readonly WorkspaceBoundary _boundary;

    public DocumentTools(string root)
    {
        _boundary = new WorkspaceBoundary(root);
    }

    [Description("Read the text of a PDF, Word document (.docx) or spreadsheet (.xlsx, .xls, .csv) in the workspace, with " +
        "page and sheet markers. Long documents come in parts: pass the startIndex the result gives to read on. Scanned " +
        "PDFs (images only) have no text. Use ReadWorkspaceFile for plain text files.")]
    public string ReadDocument(
        [Description("Workspace-relative file path.")] string relativePath,
        [Description("Maximum characters to return (default 12000, max 30000)")] int maxChars = 12_000,
        [Description("Character offset to continue from, as given in a previous result (default 0)")] int startIndex = 0)
    {
        string path = _boundary.Resolve(relativePath);
        if (SensitiveFiles.IsSensitive(path)) return MessageText.Format(ToolMessage.SensitiveRead);
        if (!File.Exists(path)) return MessageText.Format(ToolMessage.FileNotFound, new { Path = relativePath });

        string extension = Path.GetExtension(path);
        if (!DocumentText.CanRead(extension))
            return MessageText.Format(DocumentToolMessage.DocumentTypeUnsupported, new { Extension = extension, Supported = string.Join(", ", DocumentText.Extensions) });
        if (new FileInfo(path).Length > MaxDocumentBytes)
            return MessageText.Format(DocumentToolMessage.DocumentTooLarge, new { Megabytes = MaxDocumentBytes / 1_000_000 });

        string text;
        try
        {
            using var file = File.OpenRead(path);
            text = DocumentText.Read(file, extension);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return MessageText.Format(ToolMessage.DocumentUnreadable, new { Path = relativePath, Reason = ex.Message });
        }
        if (text.Length == 0) return MessageText.Format(DocumentToolMessage.DocumentNoText, new { Path = relativePath });

        maxChars = Math.Clamp(maxChars, 500, 30_000);
        startIndex = Math.Clamp(startIndex, 0, text.Length);
        int end = Math.Min(text.Length, startIndex + maxChars);
        return text[startIndex..end] +
               (end < text.Length ? MessageText.Format(DocumentToolMessage.DocumentContinues, new { Start = startIndex, End = end, text.Length }) : "");
    }

    [Description("Read a spreadsheet (.xlsx, .xls) or CSV as a table: one sheet's rows, the first as the header, a page at a " +
        "time. Says how many rows and columns there are and which sheets exist. For counting or calculating over the rows, " +
        "use it to see the shape, then a script.")]
    public string ReadTable(
        [Description("Workspace-relative file path.")] string relativePath,
        [Description("The sheet's name (default: the first sheet)")] string sheet = "",
        [Description("First data row to show, after the header (default 0)")] int startRow = 0,
        [Description("Rows to show (1-200, default 50)")] int maxRows = 50)
    {
        string path = _boundary.Resolve(relativePath);
        if (SensitiveFiles.IsSensitive(path)) return MessageText.Format(ToolMessage.SensitiveRead);
        if (!File.Exists(path)) return MessageText.Format(ToolMessage.FileNotFound, new { Path = relativePath });
        string extension = Path.GetExtension(path);
        if (!DocumentText.TableExtensions.Contains(extension.ToLowerInvariant()))
            return MessageText.Format(DocumentToolMessage.TableTypeUnsupported, new { Extension = extension, Supported = string.Join(", ", DocumentText.TableExtensions) });
        if (new FileInfo(path).Length > MaxDocumentBytes)
            return MessageText.Format(DocumentToolMessage.DocumentTooLarge, new { Megabytes = MaxDocumentBytes / 1_000_000 });

        (IReadOnlyList<string> Sheets, string Sheet, List<string[]>? Rows) table;
        try
        {
            using var file = File.OpenRead(path);
            table = DocumentText.Table(file, extension, sheet);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return MessageText.Format(ToolMessage.DocumentUnreadable, new { Path = relativePath, Reason = ex.Message });
        }
        string sheets = string.Join(", ", table.Sheets);
        if (table.Rows is null) return MessageText.Format(DocumentToolMessage.TableSheetUnknown, new { Sheet = sheet, Sheets = sheets });
        if (table.Rows.Count == 0) return MessageText.Format(DocumentToolMessage.TableEmpty, new { table.Sheet });

        string[] header = table.Rows[0];
        var data = table.Rows.Skip(1).ToList();
        int columns = table.Rows.Max(r => r.Length);
        startRow = Math.Clamp(startRow, 0, Math.Max(0, data.Count - 1));
        var shown = data.Skip(startRow).Take(Math.Clamp(maxRows, 1, 200)).ToList();

        var text = new StringBuilder();
        text.AppendLine(MessageText.Format(DocumentToolMessage.TableShown, new
        {
            table.Sheet, Sheets = sheets, Rows = data.Count, Columns = columns, First = shown.Count == 0 ? 0 : startRow + 1, Last = startRow + shown.Count,
        }));
        text.Append(MarkdownTable.Render(header, shown, columns));
        if (startRow + shown.Count < data.Count)
            text.Append(MessageText.Format(DocumentToolMessage.TableContinues, new { Next = startRow + shown.Count }));
        return ProcessRunner.Truncate(text.ToString().TrimEnd());
    }
}
