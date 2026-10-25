namespace LouisAgent.Core.Pricing.Messages;

using System.ComponentModel;

// The message enums (docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger). Members are added
// as code is written or moved; the refactor after F4 moves the existing messages here.

/// <summary>Exception texts for the prices and rates area: what is wrong and how to fix it.</summary>
public enum PricingErrorMessage
{
    [Description("Exchange rates {Source} are not in rates.json format: {Reason}")]
    RatesNotRatesJson,

    [Description("Exchange rates {Source} are empty.")]
    RatesEmpty,

    [Description("Exchange rates {Source}: base '{Base}' is not a currency code.")]
    RatesBaseNotACurrency,

    [Description("Exchange rates {Source}: '{Code}' is not a currency code.")]
    RatesCodeNotACurrency,

    [Description("Exchange rates {Source}: the {Code} rate must be positive.")]
    RateNotPositive,

    [Description("Frankfurter rates {Source}: amount must be positive.")]
    FrankfurterAmountNotPositive,

    [Description("Frankfurter rates {Source} are not in Frankfurter's format: {Reason}")]
    FrankfurterNotItsFormat,

    [Description("Price table {Source} is not valid: {Reason}")]
    PriceTableNotValid,

    [Description("Price table {Source} is empty.")]
    PriceTableEmpty,

    [Description("Price table {Source}: '{Model}' has a negative {Price} price.")]
    PriceTableNegativePrice,

    [Description("Price table {Source}: '{Model}' long_prompt.above_tokens must be positive.")]
    PriceTableLongPromptNotPositive,

    [Description("PRICES_URL must be an http or https URL, got '{Url}'.")]
    PricesUrlInvalid,

    [Description("RATES_URL must be an http or https URL (or off), got '{Url}'.")]
    RatesUrlInvalid,

    [Description("Exchange rates {Source} are from {Base}, but the price table is in {Currency}: set RATES_URL to rates from {Currency}.")]
    RatesWrongBase,

    [Description("RATES_FORMAT must be '{Frankfurter}' or '{Json}', got '{Format}'.")]
    RatesFormatUnknown,
}
