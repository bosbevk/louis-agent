using LouisAgent.Evals.Contracts.Runs;

namespace LouisAgent.Evals.Contracts.Api;

/// <summary>
/// An eval's runs, as the run list has them, asked which is the run before another, which finished last, and which
/// criteria they missed most. Every page asks it the same way, so "better or worse" means one thing everywhere.
/// </summary>
/// <remarks>
/// Louis's pick, 2026-11-23, "Run before; re-grade vs its own": a re-grade is compared with the run it re-graded, and
/// a live run or a replay with the newest earlier run of any kind that finished (Braintrust's baseline, the most recent
/// experiment), over comparing with the last live run only and over a baseline a person pins. A run that stopped or
/// couldn't finish holds only part of its conversations, so it has no run before and is never one.
/// </remarks>
/// <param name="Rows">The eval's runs in short, in any order.</param>
/// <seealso href="https://www.braintrust.dev/docs/evaluate/compare-experiments">Braintrust: compare experiments</seealso>
public sealed record RunHistory(IReadOnlyList<RunRow> Rows)
{
    /// <summary>How many of the last finished runs a page looks back over: the last eight.</summary>
    public const int Recent = 8;

    /// <summary>
    /// The run this one is compared with: the run a re-grade re-graded, or the newest earlier run that finished; none
    /// for a run that didn't finish, or with no such run.
    /// </summary>
    public RunRow? FindBefore(RunRow run)
    {
        if (run.Status != RunStatus.Finished) return null;
        if (run.RegradeOf is { } graded) return Finished().FirstOrDefault(row => row.Id == graded);

        return Finished().Where(row => row.StartedAt < run.StartedAt).MaxBy(row => row.StartedAt);
    }

    /// <summary>The last runs that finished, this many at most, the oldest first.</summary>
    public IReadOnlyList<RunRow> FindLastFinished(int count) =>
        [.. Finished().OrderByDescending(row => row.StartedAt).Take(count).Reverse()];

    /// <summary>
    /// The criteria the last runs that finished missed or a person flipped, each added up by its words, the most
    /// missed first, then the most flipped.
    /// </summary>
    public IReadOnlyList<CriterionTally> TallyMissedMost(int count) =>
    [
        .. FindLastFinished(count).SelectMany(row => row.Criteria)
            .GroupBy(tally => tally.Text)
            .Select(same => new CriterionTally(same.Key, same.Sum(tally => tally.Missed),
                same.Sum(tally => tally.Flipped)))
            .OrderByDescending(tally => tally.Missed)
            .ThenByDescending(tally => tally.Flipped)
            .ThenBy(tally => tally.Text, StringComparer.Ordinal)
    ];

    /// <summary>The runs that finished: the only ones a run is compared with.</summary>
    private IEnumerable<RunRow> Finished() => Rows.Where(row => row.Status == RunStatus.Finished);
}
