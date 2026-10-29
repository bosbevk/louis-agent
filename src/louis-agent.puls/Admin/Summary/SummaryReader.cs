using LouisAgent.Api.Contracts.Admin.Runs;

namespace LouisAgent.Puls.Admin.Summary;

/// <summary>
/// A run's roll-up from its summary rows (F18 steps 5c.2 and 5c.3): the runner's lines with the ledger's costs, as
/// standard records, not an eval's score keys. A run from before 5c has none (null, never an empty list: the portal
/// says so and offers the re-score). The modes keep the order their lines came in, each mode's overall line first.
/// </summary>
public static class SummaryReader
{
    /// <summary>The run's slices grouped by mode; none for a run from before 5c.</summary>
    public static IReadOnlyList<ModeSlices>? Read(IReadOnlyList<SummaryRow>? summaries) =>
        summaries?.Select(SliceView.Of).GroupBy(slice => slice.Mode)
            .Select(mode => new ModeSlices(mode.Key, [.. mode])).ToList();
}
