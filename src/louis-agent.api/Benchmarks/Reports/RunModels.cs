using LouisAgent.Api.Benchmarks.Runs;

namespace LouisAgent.Api.Benchmarks.Reports;

/// <summary>
/// The models a run declared for its modes and the models its ledger rows name (F18 step 2a): a row on a model the run
/// doesn't list is flagged, so a trend never silently mixes what the run didn't say it ran.
/// </summary>
public static class RunModels
{
    /// <summary>The models the ledger's rows name that the run's declared models don't list; none when the run declared none.</summary>
    public static IReadOnlyList<string> Unlisted(IReadOnlyList<BenchmarkProtocol.ModelInfo>? declared, IEnumerable<string?> rowModels)
    {
        // "unknown" declares nothing: a mode whose model the harness didn't know names no model to check the rows against.
        var listed = declared?.Select(m => m.Model).Where(m => m != Unknown).ToHashSet();
        if (listed is null || listed.Count == 0) return [];
        return rowModels.OfType<string>().Where(m => !listed.Contains(m)).Distinct().ToList();
    }

    private const string Unknown = "unknown";
}
