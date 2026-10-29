using System.Globalization;

namespace LouisAgent.Puls.Admin.Rates;

/// <summary>The words of a mode's rates (F18 step 5a): what the portal writes for pass@k, pass^k and the trials left out. A rate no trial made is a dash, never 0.</summary>
public static class RatesText
{
    public static string PassAt(int? k) => $"pass@{(k is { } n ? n.ToString(CultureInfo.InvariantCulture) : "k")}";

    public static string PassHat(int? k) => $"pass^{(k is { } n ? n.ToString(CultureInfo.InvariantCulture) : "k")}";

    public static string Share(double? rate) =>
        rate is { } value ? value.ToString("0%", CultureInfo.InvariantCulture) : "–";

    public static string LeftOut(int count) => $"{count} left out (infrastructure)";

    public static string Retried(int count) => $"{count} retried";
}
