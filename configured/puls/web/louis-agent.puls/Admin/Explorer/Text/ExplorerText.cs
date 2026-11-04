using System.Globalization;
using LouisAgent.Web.Shared.Usage;

namespace LouisAgent.Puls.Admin.Explorer.Text;

/// <summary>How the explorer writes its numbers: money, seconds, counts, and a question cut to fit a heading.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0007-measure-every-request.md">ADR 0007: measure every request</seealso>
public static class ExplorerText
{
    /// <summary>A cost in the chosen currency, or a dash when nobody knows it (never a zero).</summary>
    public static string Money(decimal? usd, CurrencyChoice? currency) =>
        usd is { } c ? UsageFormat.Money(c, "USD", currency) : "—";

    /// <summary>Seconds with one decimal.</summary>
    public static string Secs(double seconds) => seconds.ToString("0.0", CultureInfo.InvariantCulture);

    /// <summary>A count with thousands separators, or a dash when the ledger didn't know it.</summary>
    public static string Count(long? n) => n is { } v ? v.ToString("N0") : "—";

    /// <summary>A question cut short to fit a heading.</summary>
    /// <remarks>
    /// A switch on the shape of the question instead of a ternary inside a ternary: one arm for each case (nothing,
    /// short enough, too long), read like a table.
    /// </remarks>
    public static string Blurb(string? question) => question switch
    {
        null or "" => "",
        { Length: <= 56 } => question,
        _ => question[..55].TrimEnd() + "…"
    };
}
