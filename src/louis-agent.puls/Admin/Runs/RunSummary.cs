namespace LouisAgent.Puls.Admin.Runs;

/// <summary>A run's cost and requests as the runs list sends them; <see cref="Cost"/> is null when a price was unknown, and 0 only when nothing was asked.</summary>
public sealed record TotalSummary(decimal? Cost, int Requests);

/// <summary>
/// One stored run in the Benchmarks page's list (<c>GET /v1/admin/benchmarks</c>, <c>runs[]</c>). <see cref="RescoreOf"/> (F18 step 5b.2) is the run it
/// re-scored, null for a run that asked the model.
/// </summary>
public sealed record RunSummary(
    string Id,
    string Check,
    int Repeat,
    string Status,
    DateTimeOffset StartedAt,
    TotalSummary? Total,
    string? StoreError,
    IReadOnlyList<string>? Models = null,
    string? RescoreOf = null);
