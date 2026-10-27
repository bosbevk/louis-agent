using System.Globalization;

namespace LouisAgent.Puls.Admin.Charts;

/// <summary>The numbers the chart markup writes (F18 step 6a.3b, out of RunExplorer): always invariant, so a style attribute never carries a comma.</summary>
public static class ChartNumbers
{
    /// <summary>A share as a percentage to one decimal, "0" against nothing.</summary>
    public static string Percent(decimal part, decimal whole) => whole <= 0 ? "0" : Math.Round(part / whole * 100, 1).ToString(CultureInfo.InvariantCulture);

    public static string Percent(long part, long whole) => Percent((decimal)part, whole);

    /// <summary>A number for an SVG or style attribute, to three places.</summary>
    public static string Format(double v) => Math.Round(v, 3).ToString(CultureInfo.InvariantCulture);
}
