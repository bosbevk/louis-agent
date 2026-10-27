namespace LouisAgent.Api.Contracts.Admin;

/// <summary>One point of a category's line: a run, and the runner's rates for the slice.</summary>
public sealed record CategoryPointRow(
    string Run,
    DateTimeOffset At,
    double? PassRate,
    double? PassAtK,
    double? PassHatK,
    int N);

/// <summary>One line of a category's chart: a mode on its model.</summary>
public sealed record CategoryLineRow(string Name, string? Model, IReadOnlyList<CategoryPointRow> Points);

/// <summary>
/// A category's chart as the trend endpoint sends it (F18 step 5c.4): its name and its lines under <c>modes</c>, the
/// shape the overall trend's lines have, so the portal's trend chart draws it. A slice with too few trials is not a
/// point.
/// </summary>
public sealed record CategoryTrendRow(string Category, IReadOnlyList<CategoryLineRow> Modes);
