using LouisAgent.Api.Evals.Runs.Protocol;

namespace LouisAgent.Api.Evals.Reports;

/// <summary>
/// Compares the models a run said it used with the models its ledger rows name. A row on a model the run didn't list
/// gets flagged, so a trend never quietly mixes in a model nobody declared.
/// </summary>
public static class RunModels
{
    /// <summary>
    /// The models the ledger used that the run never declared. A run that declared none has nothing to check against.
    /// </summary>
    public static IReadOnlyList<string> Unlisted(IReadOnlyList<EvalProtocol.ModelInfo>? declared,
        IEnumerable<string?> rowModels)
    {
        // A model that is null is unknown: it declares nothing to check the rows against.
        var listed = declared?.Where(m => m.Model is not null).Select(m => m.Model!).ToHashSet();
        if (listed is null || listed.Count == 0) return [];
        return rowModels.OfType<string>().Where(m => !listed.Contains(m)).Distinct().ToList();
    }
}
