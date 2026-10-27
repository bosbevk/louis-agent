using System.Text.Json;

namespace LouisAgent.Puls.Admin.Summary;

/// <summary>One mode's line in a category's card: the latest run's rates (the numbers to read), and how many runs stand behind the line.</summary>
public sealed record CategoryLine(
    string Name,
    string? Model,
    int Runs,
    string? LatestRun,
    double? PassAtK,
    double? PassHatK,
    int N);

/// <summary>A category as its card shows it: its lines, the distinct runs behind them, and whether any line has two points to join (a trend).</summary>
public sealed record CategoryCard(
    string Name,
    IReadOnlyList<CategoryLine> Lines,
    int Runs,
    bool HasTrend,
    JsonElement Chart);

/// <summary>
/// Reads one category of the trend endpoint's <c>categories</c> (F18 step 5c.4) into what its card says (the rework of 2026-10-26): per mode, the latest run's pass@k and
/// pass^k and the runs behind it; nothing is worked out beyond picking the latest point by time. A card with one run has no trend to draw, and says so instead.
/// </summary>
public static class CategoryTrendReader
{
    public static CategoryCard Read(JsonElement category)
    {
        var lines = new List<CategoryLine>();
        var runs = new HashSet<string>(StringComparer.Ordinal);
        if (category.TryGetProperty("modes", out var modes) && modes.ValueKind == JsonValueKind.Array)
            foreach (var mode in modes.EnumerateArray())
            {
                var points = mode.TryGetProperty("points", out var list) && list.ValueKind == JsonValueKind.Array
                    ? list.EnumerateArray().ToList()
                    : [];
                var latest = points.Count == 0
                    ? default
                    : points.OrderBy(p =>
                        p.TryGetProperty("at", out var at) ? at.GetDateTimeOffset() : DateTimeOffset.MinValue).Last();
                foreach (var point in points)
                    if (Text(point, "run") is { } run)
                        runs.Add(run);
                lines.Add(new CategoryLine(Text(mode, "name") ?? "", Text(mode, "model"), points.Count,
                    latest.ValueKind == JsonValueKind.Object ? Text(latest, "run") : null,
                    latest.ValueKind == JsonValueKind.Object ? Number(latest, "pass_at_k") : null,
                    latest.ValueKind == JsonValueKind.Object ? Number(latest, "pass_hat_k") : null,
                    latest.ValueKind == JsonValueKind.Object && latest.TryGetProperty("n", out var n) &&
                    n.ValueKind == JsonValueKind.Number
                        ? n.GetInt32()
                        : 0));
            }

        return new CategoryCard(Text(category, "category") ?? "", lines, runs.Count, lines.Any(line => line.Runs >= 2),
            category);
    }

    private static string? Text(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static double? Number(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : null;
}
