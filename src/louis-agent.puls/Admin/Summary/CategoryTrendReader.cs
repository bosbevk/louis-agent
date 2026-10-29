using LouisAgent.Api.Contracts.Admin.Trends;

namespace LouisAgent.Puls.Admin.Summary;

/// <summary>
/// One mode's line in a category's card: the latest run's rates (the numbers to read), and how many runs stand behind
/// the line.
/// </summary>
public sealed record CategoryLine(
    string Name,
    string? Model,
    int Runs,
    string? LatestRun,
    double? PassAtK,
    double? PassHatK,
    int N);

/// <summary>
/// A category as its card shows it: its lines, the distinct runs behind them, whether any line has two points to join
/// (a trend), and the lines in the trend chart's shape for drawing it.
/// </summary>
public sealed record CategoryCard(
    string Name,
    IReadOnlyList<CategoryLine> Lines,
    int Runs,
    bool HasTrend,
    IReadOnlyList<TrendLineRow> Chart);

/// <summary>
/// Reads one of the trend's category rows (F18 step 5c.4) into what its card says (the rework of 2026-10-26): per
/// mode, the latest run's pass@k and pass^k and the runs behind it; nothing is worked out beyond picking the latest
/// point by time. A card with one run has no trend to draw, and says so instead.
/// </summary>
public static class CategoryTrendReader
{
    /// <summary>The card a category row describes.</summary>
    public static CategoryCard Read(CategoryTrendRow category)
    {
        var lines = category.Modes.Select(LineOf).ToList();
        int runs = category.Modes.SelectMany(m => m.Points).Select(p => p.Run).Distinct(StringComparer.Ordinal)
            .Count();
        return new CategoryCard(category.Category, lines, runs, lines.Any(line => line.Runs >= 2),
            [.. category.Modes.Select(ChartLineOf)]);
    }

    /// <summary>A mode's line: its latest point by time, and how many runs it has.</summary>
    private static CategoryLine LineOf(CategoryLineRow mode) =>
        mode.Points.OrderBy(p => p.At).LastOrDefault() is { } latest
            ? new CategoryLine(mode.Name, mode.Model, mode.Points.Count, latest.Run, latest.PassAtK, latest.PassHatK,
                latest.N)
            : new CategoryLine(mode.Name, mode.Model, 0, null, null, null, 0);

    /// <summary>A mode's line in the trend chart's shape: its pass rates, nothing a category doesn't measure.</summary>
    private static TrendLineRow ChartLineOf(CategoryLineRow mode) =>
        new(mode.Name, [
            .. mode.Points.Select(p => new TrendPointRow(p.Run, p.At, null, null, p.N, null, null, null,
                null, null, null, null, p.PassAtK, p.PassHatK, null))
        ]);
}
