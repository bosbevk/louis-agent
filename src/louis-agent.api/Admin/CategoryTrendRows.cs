using LouisAgent.Api.Benchmarks.Reports;

namespace LouisAgent.Api.Admin;

/// <summary>One point of a category's line: a run, and the runner's rates for the slice.</summary>
internal sealed record CategoryPointRow(
    string Run,
    DateTimeOffset At,
    double? PassRate,
    double? PassAtK,
    double? PassHatK,
    int N);

/// <summary>One line of a category's chart: a mode on its model.</summary>
internal sealed record CategoryLineRow(string Name, string? Model, IReadOnlyList<CategoryPointRow> Points);

/// <summary>
/// A category's chart as the trend endpoint sends it (F18 step 5c.4): its name and its lines under <c>modes</c>, the shape the overall trend's lines have, so the portal's trend chart draws it.
/// Records the serializer writes (ADR 0041), with the names <c>AdminJson.SnakeCase</c> gives them. A slice with too few trials is not a point.
/// </summary>
internal sealed record CategoryTrendRow(string Category, IReadOnlyList<CategoryLineRow> Modes)
{
    public static IReadOnlyList<CategoryTrendRow> Of(IReadOnlyList<CategoryTrends.Category> categories) =>
    [
        .. categories.Select(category => new CategoryTrendRow(category.Name,
        [
            .. category.Lines.Select(line => new CategoryLineRow(line.Name, line.Model,
            [
                .. line.Points.Select(p => new CategoryPointRow(p.Run, p.At, p.PassRate, p.PassAtK, p.PassHatK, p.N))
            ]))
        ]))
    ];
}
