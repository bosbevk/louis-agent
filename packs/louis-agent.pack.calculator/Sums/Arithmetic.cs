using System.Data;
using System.Globalization;
using System.Text;

namespace LouisAgent.Packs.Calculator.Sums;

/// <summary>
/// Works out one sum in code, to four decimals, which is finer than anyone cooks or shops by. A sum it can't work out
/// is refused, so nothing comes back that could be mistaken for a result.
/// </summary>
/// <remarks>
/// Reuses .NET's own expression evaluator (<see cref="DataTable.Compute"/>) rather than a package or a parser of our
/// own (ADR 0030). That evaluator also does strings, comparisons and functions, so only arithmetic's characters get
/// through to it, and a model's sum can't become anything else (ADR 0004).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0080-a-tool-when-it-improves-the-prompt.md">ADR 0080: a tool when it improves the prompt</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0004-arguments-as-data.md">ADR 0004: arguments as data</seealso>
internal static class Arithmetic
{
    /// <summary>All a sum may hold.</summary>
    private const string Allowed = "0123456789.+-*/() ";

    /// <summary>Longer than any real sum, and short enough to keep a model's mistake cheap.</summary>
    private const int LongestSum = 200;

    /// <summary>The answer in plain digits, with a point and no thousands, or false with nothing to use.</summary>
    public static bool TryWorkOut(string sum, out string result)
    {
        result = "";
        if (!IsArithmetic(sum)) return false;

        try
        {
            var table = new DataTable { Locale = CultureInfo.InvariantCulture };
            decimal value = Convert.ToDecimal(table.Compute(AsDecimals(sum), null), CultureInfo.InvariantCulture);
            result = Math.Round(value, 4).ToString("0.####", CultureInfo.InvariantCulture);
            return true;
        }
        // A sum that doesn't parse, divides by zero or outgrows a number: the evaluator throws, and each is a no.
        catch (Exception ex) when (ex is InvalidExpressionException or ArithmeticException or InvalidCastException)
        {
            return false;
        }
    }

    /// <summary>
    /// Gives every whole number a point. The evaluator reads a whole number as an int, and 100000*100000 outgrows one.
    /// </summary>
    private static string AsDecimals(string sum)
    {
        var text = new StringBuilder();
        var number = new StringBuilder();
        foreach (char c in sum + " ")
        {
            if (char.IsAsciiDigit(c) || c == '.')
            {
                number.Append(c);
                continue;
            }

            // The number has just ended, so it goes in with its point, and then what ended it.
            if (number.Length > 0) text.Append(number).Append(number.ToString().Contains('.') ? "" : ".0");
            number.Clear();
            text.Append(c);
        }

        return text.ToString();
    }

    /// <summary>Whether there's a sum here at all, and only arithmetic's characters in it.</summary>
    private static bool IsArithmetic(string sum) =>
        !string.IsNullOrWhiteSpace(sum) && sum.Length <= LongestSum && sum.All(Allowed.Contains);
}
