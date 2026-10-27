using System.Text.Json.Serialization;
using LouisAgent.Api.Benchmarks.Reports;

namespace LouisAgent.Api.Admin;

/// <summary>One point of a category's line: a run, and the runner's rates for the slice.</summary>
internal sealed record CategoryPointRow(
    [property: JsonPropertyName("run")] string Run,
    [property: JsonPropertyName("at")] DateTimeOffset At,
    [property: JsonPropertyName("pass_rate")]
    double? PassRate,
    [property: JsonPropertyName("pass_at_k")]
    double? PassAtK,
    [property: JsonPropertyName("pass_hat_k")]
    double? PassHatK,
    [property: JsonPropertyName("n")] int N);

/// <summary>One line of a category's chart: a mode on its model.</summary>
internal sealed record CategoryLineRow(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("model")] string? Model,
    [property: JsonPropertyName("points")] IReadOnlyList<CategoryPointRow> Points);

/// <summary>
/// A category's chart as the trend endpoint sends it (F18 step 5c.4): its name and its lines under <c>modes</c>, the shape the overall trend's lines have, so the portal's trend chart draws it.
/// Records the serializer writes (ADR 0041), snake_case like the other rows. A slice with too few trials is not a point.
/// </summary>
internal sealed record CategoryTrendRow(
    [property: JsonPropertyName("category")]
    string Category,
    [property: JsonPropertyName("modes")] IReadOnlyList<CategoryLineRow> Modes)
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
