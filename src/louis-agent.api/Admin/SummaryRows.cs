using LouisAgent.Api.Benchmarks.Reports;
using LouisAgent.Api.Benchmarks.Runs;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;

namespace LouisAgent.Api.Admin;

/// <summary>
/// A stored run's roll-up as the admin endpoint sends it (F18 step 5c.2): each slice as the runner wrote it, with its cost from the ledger (<see cref="SlicePricer"/>), in snake_case like the
/// other rows. No rate is computed here. A run from before 5c has no slices and sends null, never an empty list or zeros; a rate the runner left out (too few trials) is null; the cost per
/// trial is null when unknown and the cost per pass is left out when nothing passed (ADR 0007).
/// </summary>
internal static class SummaryRows
{
    public static IReadOnlyList<Dictionary<string, object?>>? Of(IReadOnlyList<BenchmarkSlice>? slices, Total? total, IReadOnlyList<Request> requests) =>
        slices?.Select(slice => Row(slice, SlicePricer.Of(slice, total, requests))).ToList();

    private static Dictionary<string, object?> Row(BenchmarkSlice s, SlicePricer.SliceCost cost)
    {
        var row = new Dictionary<string, object?>
        {
            ["mode"] = s.Mode, ["slice"] = s.Slice, ["n"] = s.N, ["passed"] = s.Passed, ["failed"] = s.Failed, ["unknown"] = s.Unknown, ["infrastructure"] = s.Infrastructure,
            ["tasks"] = s.Tasks, ["too_few"] = s.TooFew, ["pass_rate"] = s.PassRate, ["pass_at_k"] = s.PassAtK, ["pass_hat_k"] = s.PassHatK,
            ["graders"] = s.Graders.Select(g => new { grader = g.Grader, kind = g.Kind.ToString(), passed = g.Passed, failed = g.Failed, unknown = g.Unknown }),
            ["kinds"] = s.Kinds.Select(k => new { kind = k.Kind.ToString(), passed = k.Passed, failed = k.Failed, unknown = k.Unknown }),
            ["scores"] = s.Scores.Select(c => new { key = c.Key, mean = c.Mean, n = c.N }),
            ["cost_per_trial"] = cost.PerTrial,
        };
        if (s.Passed > 0) row["cost_per_pass"] = cost.PerPass;
        return row;
    }
}
