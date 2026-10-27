using System.Globalization;
using LouisAgent.Puls.Admin.Charts;
using LouisAgent.Web.Shared.Usage;

namespace LouisAgent.Puls.Admin.Summary;

/// <summary>
/// The words of a roll-up line (F18 step 5c.3). Aggregate and slice: a slice of too few trials reads "2 of 3 passed · too few for a rate", never a percentage that looks precise and is not.
/// The rates are over the slice's scenarios while <c>n</c> counts its trials (the runner's rule), and the line says passed "of the counted", the trials an infrastructure failure
/// didn't take out. A cost is "unknown" when the ledger had an unknown price, "$0" when nothing was asked (a re-score), and a dash where it does not apply (ADR 0007).
/// </summary>
public static class SummaryText
{
    public static string Passed(SliceView slice) =>
        $"{slice.Passed} of {slice.N - slice.Infrastructure} passed" + (slice.TooFew ? " · too few for a rate" : "");

    public static string Rate(SliceView slice, double? rate) =>
        slice.TooFew || rate is not { } value ? "–" : value.ToString("0%", CultureInfo.InvariantCulture);

    /// <summary>A rate bar's width: the rate as a percentage to one decimal (6a.3c, out of the table's code-behind).</summary>
    public static string BarWidth(double rate) => $"{ChartNumbers.Format(Math.Round(rate * 100, 1))}%";

    public static string Cost(decimal? cost, CurrencyChoice? currency) =>
        cost is { } amount ? UsageFormat.Money(amount, "USD", currency) : "unknown";

    public static string CostPerPass(SliceView slice, CurrencyChoice? currency) =>
        slice.HasCostPerPass ? Cost(slice.CostPerPass, currency) : "–";
}
