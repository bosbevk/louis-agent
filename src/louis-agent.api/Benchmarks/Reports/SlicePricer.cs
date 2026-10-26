using LouisAgent.Api.Benchmarks.Runs;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;

namespace LouisAgent.Api.Benchmarks.Reports;

/// <summary>
/// What a slice of a mode's roll-up cost, from the ledger and never from the runner (F18 step 5c.2, ADR 0007). The cost is that of the conversations of the slice's tasks in its mode, all
/// repeats; <c>BenchmarkLedger.Join</c> has already placed each ledger row on its conversation, specialists' and minions' rows included. Per trial is over every trial of the slice
/// (<see cref="BenchmarkSlice.N"/>), per pass over the passed ones, and absent (null) when nothing passed. An unknown price in any of those conversations makes the slice's cost
/// unknown, null, never 0 and never a sum of the rest; so does a run whose own total is unknown. A re-score asked nothing, so its total is a known 0 and so are its slices'.
/// </summary>
public static class SlicePricer
{
    /// <summary>A slice's cost per trial and per passed trial; null where unknown or undefined.</summary>
    public sealed record SliceCost(decimal? PerTrial, decimal? PerPass);

    public static SliceCost Of(BenchmarkSlice slice, Total? runTotal, IReadOnlyList<Request> requests)
    {
        if (runTotal is null || slice.N == 0) return new SliceCost(null, null);

        var rows = requests.Where(r => r.Turn is { } turn && turn.Mode == slice.Mode && slice.Tasks.Contains(turn.Task)).ToList();
        if (rows.Any(r => r.Cost is null)) return new SliceCost(null, null);

        decimal cost = rows.Sum(r => r.Cost!.Value);
        return new SliceCost(cost / slice.N, slice.Passed > 0 ? cost / slice.Passed : null);
    }
}
