namespace LouisAgent.Evals.Datasets;

/// <summary>
/// Which scenarios a run asks for (F18 step 5.4, <c>--split</c>; ADR 0024: parsed once where it enters, with <c>SettingChoice</c>): the
/// <c>dev</c> ones it tunes against (the default, and all the portal runs), the <c>held-out</c> ones before a release, or <c>all</c>. A scenario
/// is only ever dev or held-out (<see cref="DatasetSplit"/>), so this is the run's choice, not a scenario's field.
/// </summary>
public enum SplitSelection
{
    Dev,
    HeldOut,
    All,
}

public static class SplitSelections
{
    /// <summary>Whether a run with this selection runs a scenario of the split.</summary>
    public static bool Includes(this SplitSelection selection, DatasetSplit split) => selection switch
    {
        SplitSelection.All => true,
        SplitSelection.HeldOut => split == DatasetSplit.HeldOut,
        _ => split == DatasetSplit.Dev,
    };
}
