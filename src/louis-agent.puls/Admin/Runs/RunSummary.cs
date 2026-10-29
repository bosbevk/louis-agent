using LouisAgent.Api.Contracts.Admin.Runs;

namespace LouisAgent.Puls.Admin.Runs;

/// <summary>
/// What a run cost and how many requests it made. The cost is null when a price was unknown, and 0 only when nothing
/// was asked.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0007-measure-every-request.md">ADR 0007: measure every request</seealso>
public sealed record TotalSummary(decimal? Cost, int Requests);

/// <summary>
/// One stored run as the Benchmarks page lists it: what ran, how it ended, what it cost, the models it ran on, the
/// run it re-scored (F18 step 5b.2) or replayed (FX28; null for a run that asked the model), and whether it recorded
/// its answers, so it can be replayed.
/// </summary>
/// <remarks>
/// Fowler's <i>Replace Constructor with Factory Function</i>: <see cref="Of"/> builds the row from the contract's
/// <c>StoredRunRow</c>, so the page reads the API through the contract while the run's row, the estimate and the
/// filter keep the small shape they were written and tested for (the unattended run's finding 7).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public sealed record RunSummary(
    string Id,
    string Check,
    int Repeat,
    string Status,
    DateTimeOffset StartedAt,
    TotalSummary? Total,
    string? StoreError,
    IReadOnlyList<string>? Models = null,
    string? RescoreOf = null,
    string? ReplayOf = null,
    bool Recorded = false)
{
    /// <summary>The list's row for a stored run; a run from before totals or models were kept has none.</summary>
    public static RunSummary Of(StoredRunRow run) => new(
        run.Id,
        run.Check,
        run.Repeat,
        run.Status,
        run.StartedAt,
        run.Total is { } total ? new TotalSummary(total.Cost, total.Requests) : null,
        run.StoreError,
        run.Models?.Select(m => m.Model).OfType<string>().ToList(),
        run.RescoreOf,
        run.ReplayOf,
        run.Recorded);
}
