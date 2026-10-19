namespace LouisAgent.Core.Pricing.Rates;

using System.Globalization;
using System.Text.RegularExpressions;
using LouisAgent.Core.Config;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using Microsoft.Extensions.Logging;

/// <summary>
/// The currency costs are shown in (F14), with the rate from the price table's currency and that rate's date. Costs are
/// only converted for display: the ledger keeps what was charged.
/// </summary>
/// <param name="Code">ISO 4217 code, e.g. ZAR.</param>
/// <param name="Symbol">How amounts are written, e.g. R (from .NET's region data); the code when there's none.</param>
/// <param name="Rate">How much of <see cref="Code"/> one unit of <see cref="Base"/> buys; 1 when not converting.</param>
/// <param name="RateAsOf">The rate's date; null when not converting.</param>
/// <param name="Base">The price table's currency, what costs are charged in.</param>
/// <param name="Source">What chose the currency, for the start-up log: e.g. "HOST_CULTURE en-ZA".</param>
public sealed partial record DisplayCurrency(string Code, string Symbol, decimal Rate, string? RateAsOf, string Base, string Source)
{
    private static readonly ILogger Log = AgentLogging.For<DisplayCurrency>();

    /// <summary>Whether costs are converted, i.e. shown in another currency than they were charged in.</summary>
    public bool Converts => !string.Equals(Code, Base, StringComparison.OrdinalIgnoreCase);

    /// <summary>No conversion: costs in the price table's currency, as before F14.</summary>
    public static DisplayCurrency Charged(string baseCurrency) =>
        new(baseCurrency, SymbolFor(baseCurrency), 1m, null, baseCurrency, "price table");

    /// <summary>
    /// Chooses the currency: <c>DISPLAY_CURRENCY</c> → <c>HOST_CULTURE</c>'s region → the process's own culture's region
    /// (only when it isn't the invariant culture, so never in a container) → the price table's currency. A currency
    /// with no rate is shown in the price table's currency, with a warning, never at a guessed rate.
    /// </summary>
    /// <param name="processCulture">For tests: the process's culture (default: <see cref="CultureInfo.CurrentCulture"/>).</param>
    public static DisplayCurrency Resolve(AgentOptions agent, string baseCurrency, ExchangeRates rates, CultureInfo? processCulture = null)
    {
        CultureInfo process = processCulture ?? CultureInfo.CurrentCulture;
        (string Code, string Symbol, string Source)? chosen =
            FromSetting(agent.DisplayCurrency)
            ?? FromCulture(agent.HostCulture, "HOST_CULTURE")
            ?? FromCulture(process, $"the culture {process.Name}");

        if (chosen is not { } choice || string.Equals(choice.Code, baseCurrency, StringComparison.OrdinalIgnoreCase))
            return Charged(baseCurrency);

        if (rates.RateFor(choice.Code) is not { } rate)
        {
            Log.Log(LogLevel.Warning, LogMessage.DisplayCurrencyHasNoRate, new { choice.Code, choice.Source, Base = baseCurrency });
            return Charged(baseCurrency);
        }

        return new DisplayCurrency(choice.Code, DistinctSymbol(choice.Code, choice.Symbol, baseCurrency), rate, rates.AsOf,
            baseCurrency, choice.Source);
    }

    /// <summary>
    /// <paramref name="symbol"/>, unless it's the price table's own symbol for another currency: .NET writes AUD, CAD and
    /// NZD as "$" too, and "$0.10" in Australian dollars beside "$0.0021 charged" would read as US dollars. Then the code
    /// is used instead ("0.10 AUD").
    /// </summary>
    public static string DistinctSymbol(string code, string symbol, string baseCurrency) =>
        !string.Equals(code, baseCurrency, StringComparison.OrdinalIgnoreCase) && symbol == SymbolFor(baseCurrency) ? code : symbol;

    /// <summary>The start-up log line, e.g. "ZAR (R) from HOST_CULTURE en-ZA at 16.5331 ZAR/USD, as of 2026-10-09".</summary>
    public override string ToString() => Converts
        ? $"{Code} ({Symbol}) from {Source} at {Rate.ToString(CultureInfo.InvariantCulture)} {Code}/{Base}, as of {RateAsOf}"
        : $"{Code} (as charged)";

    private static (string, string, string)? FromSetting(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        string upper = code.Trim().ToUpperInvariant();
        if (CurrencyCode().IsMatch(upper)) return (upper, SymbolFor(upper), "DISPLAY_CURRENCY");

        Log.Log(LogLevel.Warning, LogMessage.DisplayCurrencyNotACode, new { Code = code });
        return null;
    }

    private static (string, string, string)? FromCulture(string? name, string setting)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        try
        {
            return FromCulture(CultureInfo.GetCultureInfo(name.Trim()), $"{setting} {name.Trim()}")
                   ?? Warn(LogMessage.CultureHasNoRegion);
        }
        catch (CultureNotFoundException)
        {
            return Warn(LogMessage.CultureUnknown);
        }

        (string, string, string)? Warn(LogMessage why)
        {
            Log.Log(LogLevel.Warning, why, new { Setting = setting, Name = name });
            return null;
        }
    }

    /// <summary>The currency of <paramref name="culture"/>'s region, or null for the invariant or a neutral culture.</summary>
    private static (string, string, string)? FromCulture(CultureInfo culture, string source)
    {
        if (culture.Name.Length == 0 || culture.IsNeutralCulture) return null;
        try
        {
            var region = new RegionInfo(culture.Name);
            return (region.ISOCurrencySymbol, region.CurrencySymbol, source);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    /// <summary>
    /// The symbol for a currency code: the price table's dollar keeps F3's "$"; otherwise the symbol of the first culture
    /// (by name) whose region uses it, e.g. ZAR → R, EUR → €; the code itself when .NET knows none.
    /// </summary>
    internal static string SymbolFor(string code)
    {
        if (code == "USD") return "$";
        return CultureInfo.GetCultures(CultureTypes.SpecificCultures)
            .OrderBy(c => c.Name, StringComparer.Ordinal)
            .Select(c => { try { return new RegionInfo(c.Name); } catch (ArgumentException) { return null; } })
            .FirstOrDefault(r => r?.ISOCurrencySymbol == code)?.CurrencySymbol ?? code;
    }

    [GeneratedRegex("^[A-Z]{3}$")]
    private static partial Regex CurrencyCode();
}