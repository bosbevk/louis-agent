
using System.ComponentModel;

namespace LouisAgent.Core.Tools.Currency.Messages;
// The message enums (docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger). Members are added
// as code is written or moved; the refactor after F4 moves the existing messages here.

/// <summary>Strings for the currency toolset, returned to the model: what went wrong and what to do instead.</summary>
public enum CurrencyToolMessage
{
    [Description(
        "No exchange rates are loaded on this instance (RATES_URL or RATES_FILE), so amounts can't be converted.")]
    RatesUnavailable,

    [Description("No rate for '{Currency}'. Known currencies: {Known}.")]
    CurrencyUnknown,

    [Description("{Amount} {From} = {Result} {To} (1 {From} = {Rate} {To}; rates of {AsOf}, {Source}).")]
    CurrencyConverted,
}
