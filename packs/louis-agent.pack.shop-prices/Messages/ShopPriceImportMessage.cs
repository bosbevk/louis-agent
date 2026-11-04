using System.ComponentModel;

namespace LouisAgent.Packs.ShopPrices.Messages;

/// <summary>
/// What the Stats SA upload tells the person who uploads it (ADR 0009): the problems that kept a file or a cell from
/// being read, each naming the file.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
public enum ShopPriceImportMessage
{
    [Description("The {Sheet} file couldn't be read as a spreadsheet ({Reason}).")]
    SheetUnreadable,

    [Description("The {Sheet} file has no {Column} column; is it Stats SA's {Sheet} file?")]
    ColumnMissing,

    [Description("The {Sheet} file has no month columns (M202608 and the like).")]
    NoMonths,

    [Description("Row {Row} of the {Sheet} file names a province we don't know: {Province}.")]
    ProvinceUnknown,

    [Description("Row {Row} of the {Sheet} file has a price we can't read for {Month}: {Text}.")]
    PriceUnreadable,
}
