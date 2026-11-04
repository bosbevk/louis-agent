namespace LouisAgent.Puls.Admin.Runs;

/// <summary>
/// The runs list under the cards: the chosen check's runs, newest first as the store sends them, the models they ran
/// on, the ones the model filter shows, and what they cost together (null when no cost is known, never 0).
/// </summary>
/// <param name="Runs">Every run of the check.</param>
/// <param name="Models">The models those runs name, for the filter.</param>
/// <param name="Shown">The runs on the model picked; all of them when none is.</param>
/// <param name="Cost">The known costs of every run added up.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0007-measure-every-request.md">ADR 0007: measure every request</seealso>
public sealed record CheckRuns(
    IReadOnlyList<RunSummary> Runs,
    IReadOnlyList<string> Models,
    IReadOnlyList<RunSummary> Shown,
    decimal? Cost)
{
    /// <summary>The list for this check and model; no check chosen is an empty list.</summary>
    public static CheckRuns For(string? check, IReadOnlyList<RunSummary> runs, string? model)
    {
        var mine = check is null ? [] : runs.Where(r => r.Check == check).ToList();
        var costs = mine.Select(r => r.Total?.Cost).OfType<decimal>().ToList();
        return new CheckRuns(
            mine,
            RunModelFilter.ModelsOf(mine.Select(r => r.Models)),
            mine.Where(r => RunModelFilter.Matches(r.Models, model)).ToList(),
            costs.Count > 0 ? costs.Sum() : null);
    }
}
