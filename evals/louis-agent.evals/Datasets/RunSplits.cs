using LouisAgent.Evals.Datasets.Scenarios;
using LouisAgent.Evals.Protocol.Kinds;

namespace LouisAgent.Evals.Datasets;

/// <summary>
/// What a run's split means for the dataset: which of its scenarios a run asks for. The contract knows the split, but
/// not a scenario's own split, so this rule stays with the datasets.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0043-repeated-names-are-enums.md">ADR 0043: repeated names are enums</seealso>
public static class RunSplits
{
    /// <summary>
    /// The old way to write a held-out split (<c>held_out</c>), which a command line, a stored run and the API's v1
    /// lines still use, so it keeps reading.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, RunSplit> Aliases =
        new Dictionary<string, RunSplit> { ["held_out"] = RunSplit.HeldOut };

    /// <summary>Whether a run with this split asks for a scenario of the dataset's split.</summary>
    /// <remarks>A switch expression on the split, one arm per case, read like a table.</remarks>
    public static bool Includes(this RunSplit split, DatasetSplit scenario) => split switch
    {
        RunSplit.All => true,
        RunSplit.HeldOut => scenario == DatasetSplit.HeldOut,
        _ => scenario == DatasetSplit.Dev,
    };
}
