
using System.ComponentModel;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Pricing.Rates;
using LouisAgent.Core.Tools.Currency.Messages;

namespace LouisAgent.Core.Tools.Currency;
/// <summary>
/// Converting amounts at the day's rates (F22 step 5; S, CL15, from WebTools): listed by the web toolset, since a
/// price found on the web is what gets converted, but no web of its own.
/// </summary>
public sealed class CurrencyTools(ExchangeRates? rates = null)
{
    private readonly ExchangeRates _rates = rates ?? ExchangeRates.Empty;

    [Description(
        "Convert an amount between currencies at the day's reference rates (the same rates the app shows costs in). " +
        "Codes like ZAR, USD, EUR, GBP.")]
    public string ConvertCurrency(
        [Description("The amount to convert")] decimal amount,
        [Description("The currency it's in, e.g. ZAR")]
        string from,
        [Description("The currency to convert to, e.g. USD")]
        string to)
    {
        if (_rates.IsEmpty) return MessageText.Format(CurrencyToolMessage.RatesUnavailable);
        string fromCode = (from ?? "").Trim().ToUpperInvariant(), toCode = (to ?? "").Trim().ToUpperInvariant();
        string known = string.Join(", ", _rates.Rates.Keys.Append(_rates.Base).Order());
        if (_rates.RateFor(fromCode) is not { } fromRate || fromRate == 0)
            return MessageText.Format(CurrencyToolMessage.CurrencyUnknown, new { Currency = from, Known = known });
        if (_rates.RateFor(toCode) is not { } toRate)
            return MessageText.Format(CurrencyToolMessage.CurrencyUnknown, new { Currency = to, Known = known });

        // Rates are base → currency, so from → to goes through the base.
        decimal rate = toRate / fromRate;
        var invariant = System.Globalization.CultureInfo.InvariantCulture;
        return MessageText.Format(CurrencyToolMessage.CurrencyConverted, new
        {
            Amount = amount.ToString("N2", invariant),
            From = fromCode,
            Result = (amount * rate).ToString("N2", invariant),
            To = toCode,
            Rate = rate.ToString("0.######", invariant),
            _rates.AsOf,
            Source = _rates.Source ?? "rates.json",
        });
    }
}
