using System.Globalization;
using System.Text.RegularExpressions;

namespace LouisAgent.Identities.Puls.Evals.Checks.Cook.Scoring;

/// <summary>
/// Reads the rand amounts in a text the way South African answers write them: `R149,95`, `R149.95`, `R1 249,95`,
/// `R1,249.95`, `R1.249,95`, `R150`. Stricter than <see cref="Prices"/>' counting regex, which only has to count.
/// </summary>
/// <remarks>
/// An amount is whole groups of three digits with a separator, or plain digits, then at most two decimals; the text
/// after it (`2 x`, `250 g`) isn't part of it. A pack's weight or volume after the price is no group of the amount:
/// `R45 250 g` is R45 for 250 g, so a grouped reading that a unit follows is refused and the plain digits are taken.
/// Only the grouped reading is checked: a multiplier after a price (`R69,99 x 2`) is no unit, so the price stays whole.
/// </remarks>
public static partial class Money
{
    /// <summary>
    /// One amount: <c>R</c>, the whole rands (grouped unless a unit follows, else plain digits), then up to two cents.
    /// Matches "R1 249,95"; doesn't take "250" in "R45 250 g" as part of the amount.
    /// </summary>
    [GeneratedRegex(
        @"\bR\s?(?:(?<whole>\d{1,3}(?:[ \u00A0.,]\d{3})+)(?!\s*(?:kg|g|gram|ml|l|liter|litre)\b)|(?<whole>\d+))(?:[.,](?<cents>\d{1,2}))?(?!\d)",
        RegexOptions.CultureInvariant)]
    private static partial Regex Amount();

    /// <summary>Every amount in the text, in order.</summary>
    public static IReadOnlyList<decimal> Amounts(string? text) =>
        Amount().Matches(text ?? "").Select(Value).ToList();

    /// <summary>One match as a decimal, whatever separators the answer used.</summary>
    private static decimal Value(Match match)
    {
        string whole = new(match.Groups["whole"].Value.Where(char.IsDigit).ToArray());
        string cents = match.Groups["cents"].Success ? match.Groups["cents"].Value.PadRight(2, '0') : "00";
        return decimal.Parse(whole + "." + cents, CultureInfo.InvariantCulture);
    }
}
