namespace LouisAgent.Core.Tools.Documents.Messages;

using System.ComponentModel;

// The message enums (docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger). Members are added
// as code is written or moved; the refactor after F4 moves the existing messages here.

/// <summary>Strings for the document toolset (tables and documents), returned to the model: what went wrong and what to do instead.</summary>
public enum DocumentToolMessage
{
    [Description("ReadTable reads {Supported}; '{Extension}' isn't one of them.")]
    TableTypeUnsupported,

    [Description("No sheet named '{Sheet}'. Sheets: {Sheets}.")]
    TableSheetUnknown,

    [Description("Sheet '{Sheet}' is empty.")]
    TableEmpty,

    [Description("Sheet '{Sheet}' (sheets: {Sheets}): {Rows} rows × {Columns} columns; rows {First}-{Last} below, the first row as the header.")]
    TableShown,

    [Description("\n[More rows: call ReadTable with startRow={Next} to continue.]")]
    TableContinues,

    [Description("ReadDocument reads {Supported}; '{Extension}' isn't one of them. Use ReadWorkspaceFile for text files.")]
    DocumentTypeUnsupported,

    [Description("{Path} has no text to read: a scanned PDF holds only images, and this tool has no OCR.")]
    DocumentNoText,

    [Description("Document is larger than the {Megabytes} MB reading limit.")]
    DocumentTooLarge,

    [Description("\n[Showing characters {Start}-{End} of {Length}. Call ReadDocument with startIndex={End} to continue.]")]
    DocumentContinues,
}
