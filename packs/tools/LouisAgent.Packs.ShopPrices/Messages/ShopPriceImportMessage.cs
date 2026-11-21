using System.ComponentModel;

namespace LouisAgent.Packs.ShopPrices.Messages;

/// <summary>
/// What the Stats SA upload tells the person who uploads it (ADR 0009): what it is, the files it takes, the facts its
/// preview shows (each a label and a value), and the problems that keep it from saving, each naming its file.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
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

    [Description("Stats SA's average prices (P0141): the two spreadsheets from statssa.gov.za, all urban areas and the provinces. Each upload adds the months we don't have and Stats SA's corrections to the ones we do.")]
    Description,

    [Description("All urban areas: CPI_Average Prices_All urban (yyyymm).xlsx")]
    AllUrbanSlot,

    [Description("Provinces: CPI_Average prices_Provinces (yyyymm).xlsx")]
    ProvincesSlot,

    [Description("No {Sheet} file was given.")]
    FileMissing,

    [Description("No price store is set (SHOP_PRICES_MONGO_URL), so nothing can be saved; the preview still shows what the files hold.")]
    StoreCantSave,

    [Description("The price store didn't answer ({Reason}).")]
    StoreUnavailable,

    [Description("The files hold no prices.")]
    NoPrices,

    [Description("{First} to {Last}")]
    Period,

    [Description("Months in the files")]
    MonthsLabel,

    [Description("{Count}, {Period}")]
    MonthsValue,

    [Description("New months")]
    NewMonthsLabel,

    [Description("Months stored already, unchanged")]
    UnchangedMonthsLabel,

    [Description("Corrected: {Month}")]
    CorrectedLabel,

    [Description("{Count} changed: {Examples}")]
    CorrectedValue,

    [Description("{Product} {Size} in {Region}: R{Old} to R{New}")]
    PriceChanged,

    [Description("{Product} {Size} in {Region}: new, R{New}")]
    PriceAdded,

    [Description("{Product} {Size} in {Region}: gone, was R{Old}")]
    PriceRemoved,

    [Description("{Product}: its names changed")]
    NamesChanged,

    [Description("Products in {Month}")]
    ProductsLabel,

    [Description("Products with no names yet (add them to statssa-names.json)")]
    UnnamedLabel,

    [Description("none")]
    None,
}
