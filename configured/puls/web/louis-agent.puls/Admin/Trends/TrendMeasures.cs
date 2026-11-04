using System.Globalization;
using LouisAgent.Web.Shared.Usage;

namespace LouisAgent.Puls.Admin.Trends;

/// <summary>
/// The trend's panels, in the order the chart draws them: the measures the runs carry, then pass@k and pass^k (F18
/// step 5a.2). A value the API sent as null (nobody measured it) stays null, never 0. The run's score is the share of
/// its conversations that passed (since 7c.3a), so its panel says so; the accuracy panel went, as no grader measures
/// it (R2-3).
/// </summary>
public static class TrendMeasures
{
    /// <summary>The cost panel, the one the card's small chart draws.</summary>
    public static TrendMeasure Cost(Func<decimal, string> money) =>
        new("cost", "Cost per repeat", p => (double?)p.Cost, v => money((decimal)v));

    /// <summary>Every panel; money is written in the page's currency.</summary>
    public static IReadOnlyList<TrendMeasure> All(Func<decimal, string> money) =>
    [
        Cost(money),
        new("tokens", "Tokens per repeat", p => p.Tokens, v => UsageFormat.Tokens((long)v)),
        new("time", "Seconds per turn", p => p.SecondsPerTurn,
            v => v.ToString("0.0", CultureInfo.InvariantCulture) + " s"),
        new("passed", "Passed (conversations that passed)", p => p.Score, Percent, 1),
        new("cache", "Cache (from the cache)", p => p.CacheShare, Percent, 1),
        new("tools", "Tools (refused or failed)", p => p.ToolsFailed, Percent, 1),
        new("sources", "Sources (sites read)", p => p.Sites, v => v.ToString("0", CultureInfo.InvariantCulture)),
        new("value", "Value (cost per answer)", p => (double?)p.CostPerAnswer, v => money((decimal)v)),
        new("passat", "pass@k (passed at least once)", p => p.PassAtK, Percent, 1),
        new("passhat", "pass^k (passed every time)", p => p.PassHatK, Percent, 1),
    ];

    /// <summary>A share as a whole percentage.</summary>
    private static string Percent(double v) => v.ToString("0%", CultureInfo.InvariantCulture);
}
