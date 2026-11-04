using LouisAgent.Api.Evals.Store;
using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Evals.Reports;

/// <summary>
/// Works out what a slice of a run cost, per trial and per pass, from the ledger and never from the runner. The cost is
/// that of the slice's tasks in its mode, over all repeats, specialists' and minions' requests included.
/// </summary>
/// <remarks>
/// If any part of the price is unknown, the slice's cost is unknown too, never 0 and never the sum of the rest: an
/// unpriced request, a request that couldn't be placed on a conversation, or a run whose own total is unknown. A
/// re-score asked nothing, so its total is a known 0, and so are its slices'.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0007-measure-every-request.md">ADR 0007: measure every request</seealso>
public static class SlicePricer
{
    /// <summary>A slice's cost per trial and per passed trial; null where unknown or undefined.</summary>
    public sealed record SliceCost(decimal? PerTrial, decimal? PerPass);

    /// <summary>Prices one slice from the run's placed requests; per pass is left out when nothing passed.</summary>
    /// <remarks>Guard clauses: each reason the cost can't be known returns early, from the top.</remarks>
    public static SliceCost Of(EvalSlice slice, Total? runTotal, IReadOnlyList<Request> requests)
    {
        if (runTotal is null || slice.N == 0) return new SliceCost(null, null);

        // A row nobody could place on a conversation still counts in the run's total: pricing a slice from the rows
        // that were placed would be a sum of the rest (ADR 0007).
        if (requests.Any(r => r.Turn is null)) return new SliceCost(null, null);

        var rows = requests.Where(r => r.Turn is { } turn && turn.Mode == slice.Mode && slice.Tasks.Contains(turn.Task))
            .ToList();
        if (rows.Any(r => r.Cost is null)) return new SliceCost(null, null);

        decimal cost = rows.Sum(r => r.Cost!.Value);
        return new SliceCost(cost / slice.N, slice.Passed > 0 ? cost / slice.Passed : null);
    }
}
