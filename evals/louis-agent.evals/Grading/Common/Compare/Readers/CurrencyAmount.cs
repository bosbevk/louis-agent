using System.Globalization;
using LouisAgent.Evals.Readers;

namespace LouisAgent.Evals.Grading.Common.Compare.Readers;

/// <summary>
/// Reads the last amount an answer writes after a currency sign (R, $, € or £, with one space or none) as its total:
/// an answer lists its prices and then sums them. The decimal mark is a comma or a full stop followed by exactly two
/// digits at the end, so "R69,99" and "$1,234.56" both read; any other separator is a thousands mark.
/// </summary>
/// <remarks>
/// The plain money reader any eval can name, <c>currency-amount</c>. It knows no language's words for "total": an
/// identity that writes its totals another way ships its own reader. It walks the text a character at a time, with no
/// pattern (ADR 0057).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0057-regex-is-the-last-option.md">ADR 0057: a regular expression is the last option</seealso>
public sealed class CurrencyAmount : IMoneyReader
{
    /// <summary>The signs an amount may follow.</summary>
    private const string Signs = "R$€£";

    /// <inheritdoc />
    public string Name => "currency-amount";

    /// <inheritdoc />
    public decimal? Total(string answer)
    {
        decimal? last = null;
        for (var i = 0; i < answer.Length; i++)
            if (IsSign(answer, i) && AmountAfter(answer, i + 1) is { } amount)
                last = amount;
        return last;
    }

    /// <summary>
    /// Whether the character here is a currency sign; an R only when it doesn't end a longer word, so "BR5" isn't
    /// an amount.
    /// </summary>
    private static bool IsSign(string text, int at) =>
        Signs.Contains(text[at]) && !(text[at] == 'R' && at > 0 && char.IsLetterOrDigit(text[at - 1]));

    /// <summary>
    /// The amount that starts here, after one space at most, or null when no digit follows.
    /// </summary>
    private static decimal? AmountAfter(string text, int at)
    {
        if (at < text.Length && text[at] == ' ') at++;
        if (at >= text.Length || !char.IsAsciiDigit(text[at])) return null;

        var end = at;
        while (end < text.Length && (char.IsAsciiDigit(text[end]) || text[end] is ',' or '.')) end++;
        // A separator the number ends on is the sentence's, not the number's.
        var written = text[at..end].TrimEnd(',', '.');
        return Parse(written);
    }

    /// <summary>
    /// The written number as an amount: a last separator followed by exactly two digits is the decimal mark, and
    /// every other separator is dropped as a thousands mark.
    /// </summary>
    private static decimal? Parse(string written)
    {
        var mark = written.LastIndexOfAny([',', '.']);
        var hasCents = mark >= 0 && written.Length - mark - 1 == 2;
        var whole = (hasCents ? written[..mark] : written).Replace(",", "").Replace(".", "");
        var number = hasCents ? $"{whole}.{written[(mark + 1)..]}" : whole;
        return decimal.TryParse(number, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }
}
