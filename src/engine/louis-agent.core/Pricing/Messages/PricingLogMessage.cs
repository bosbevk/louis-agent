
using System.ComponentModel;

namespace LouisAgent.Core.Pricing.Messages;
// The message enums (docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger). Members are added
// as code is written or moved; the refactor after F4 moves the existing messages here.

/// <summary>Log lines for the prices and rates, written to the console and the run file.</summary>
public enum PricingLogMessage
{
    [Description("Display currency: no {Code} rate (from {Source}); costs are shown in {Base}.")]
    DisplayCurrencyHasNoRate,

    [Description("DISPLAY_CURRENCY '{Code}' is not a currency code (e.g. ZAR); ignoring it.")]
    DisplayCurrencyNotACode,

    [Description("{Setting} '{Name}' names no region (use a specific culture such as en-ZA); ignoring it.")]
    CultureHasNoRegion,

    [Description("{Setting} '{Name}' is not a culture .NET knows; ignoring it.")]
    CultureUnknown,

    [Description("Prices: {Source} (as of {AsOf}, {Count} models)")]
    PricesLoaded,

    [Description("Prices: {Url} can't be reached ({Reason}); using the price file instead.")]
    PricesUrlUnreachable,

    [Description("No price table at {Path}: usage is recorded without cost. Set PRICES_FILE to price it.")]
    NoPriceTable,

    [Description("Rates: {Source} (as of {AsOf}, {Count} currencies)")]
    RatesLoaded,

    [Description("Rates: {Url} can't be used ({Reason}); using the rates file instead.")]
    RatesUrlUnusable,

    [Description("No exchange rates at {Path}: costs are shown in {Base}.")]
    NoRates,
}
