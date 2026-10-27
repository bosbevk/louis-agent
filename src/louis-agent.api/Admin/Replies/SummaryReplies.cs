using LouisAgent.Api.Benchmarks.Reports;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Choices;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;

namespace LouisAgent.Api.Admin.Replies;

/// <summary>Builds a stored run's roll-up rows, each slice priced from the ledger.</summary>
/// <remarks>
/// Replace Constructor with Factory Function (Fowler), as C# 14 static extension members, for the reason
/// <c>AdminReplies</c> gives: the records live in the contract, which can't see a slice or the ledger.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0007-measure-every-request.md">ADR 0007: measure every request</seealso>
internal static class SummaryReplies
{
    /// <summary>A run's roll-up comes from the runner's slices, priced with the ledger's requests.</summary>
    extension(SummaryRow)
    {
        /// <summary>
        /// A stored run's roll-up, each slice priced from the ledger; null for a run from before 5c (never an empty
        /// list or zeros).
        /// </summary>
        public static IReadOnlyList<SummaryRow>? Of(IReadOnlyList<BenchmarkSlice>? slices, Total? total,
            IReadOnlyList<Request> requests) =>
            slices?.Select(slice => Row(slice, SlicePricer.Of(slice, total, requests))).ToList();
    }

    /// <summary>One slice's row: the runner's fields as it wrote them, and its cost.</summary>
    private static SummaryRow Row(BenchmarkSlice s, SlicePricer.SliceCost cost) => new(
        s.Mode,
        s.Slice,
        s.N,
        s.Passed,
        s.Failed,
        s.Unknown,
        s.Infrastructure,
        s.Tasks,
        s.TooFew,
        s.PassRate,
        s.PassAtK,
        s.PassHatK,
        [.. s.Graders.Select(g => new SliceGraderRow(g.Grader, g.Kind.Wire(), g.Passed, g.Failed, g.Unknown))],
        [.. s.Kinds.Select(k => new SliceKindRow(k.Kind.Wire(), k.Passed, k.Failed, k.Unknown))],
        [.. s.Scores.Select(c => new SliceScoreRow(c.Key, c.Mean, c.N))],
        cost.PerTrial,
        cost.PerPass);
}
