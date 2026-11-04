using System.ComponentModel;

namespace LouisAgent.Packs.ShopPrices.Messages;

/// <summary>
/// The shop-price tool's texts the model reads (ADR 0009), pinned by <c>ShopPriceToolMessageSnapshot.json</c>.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
public enum ShopPriceToolMessage
{
    [Description(
        "EstimatePrices needs the shopping list: one line per item, what to buy and its pack ('beef mince 500 g', " +
        "'eggs 18', 'onions 1 kg x 2').")]
    NoItems,

    [Description("At most {Max} lines at once; you sent {Count}. Send the rest in a second call.")]
    TooManyItems,

    [Description("No province '{Province}' is known, so the national averages are used. Provinces: {Provinces}.")]
    ProvinceUnknown,

    [Description(
        "Estimated prices for {Region}, from Stats SA's average prices for {Month} (averages over many shops, not " +
        "one shop's prices). The scaling and the sums are louis-agent's; copy the numbers as they are.")]
    Header,

    [Description("| Item | Priced from | Estimate |")]
    TableHeader,

    [Description("|---|---|---|")] TableRule,

    [Description("| {Item} | {Product}, {Size} at R{Price}{Where}{When}{Ours} | ± R{Rand} |")]
    LinePriced,

    [Description(
        "| {Item} | {Product}, {Size} at R{Price}{Where}{When}{Ours}; no size given, so one {Size} pack " +
        "| ± R{Rand} |")]
    LineTablePack,

    [Description("| {Item} | {Product} is priced only as {Sizes}: send the line with one of those sizes | ? |")]
    LineOtherUnit,

    [Description("| {Item} | no price for this product | ? |")]
    LineNotFound,

    [Description(" (national, no price for {Region})")]
    NationalStandsIn,

    [Description(" (our own estimate, not Stats SA's)")]
    OurOwnRow,

    [Description(" (from {Month}, the newest month that prices it)")]
    OlderMonth,

    [Description("| **Total** | {Priced} of {Count} items | **± R{Total}{LeftOut}** |")]
    TotalRow,

    [Description("| **Per person** | for {Serves} | **± R{PerPerson}{LeftOut}** |")]
    PerPersonRow,

    [Description(" + 1 item unpriced")] LeftOutOne,

    [Description(" + {Unpriced} items unpriced")]
    LeftOutMany,

    [Description(
        "No estimate for: {Items}. Write ? for their price, leave them out of the total, and say so under Unsure.")]
    Unpriced,

    [Description(
        "Credit the source under the list: \"Prices are estimates from Stats SA's average prices ({Region}, " +
        "{Month}), not checked at a shop.\"")]
    Credit,
}
