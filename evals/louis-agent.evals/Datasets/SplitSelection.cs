using LouisAgent.Choices;

namespace LouisAgent.Evals.Datasets;

/// <summary>
/// Which scenarios a run asks for (F18 step 5.4, <c>--split</c>; ADR 0024: parsed once where it enters, with <c>SettingChoice</c>): the
/// <c>dev</c> ones it tunes against (the default, and all the portal runs), the <c>held-out</c> ones before a release, or <c>all</c>. A scenario
/// is only ever dev or held-out (<see cref="DatasetSplit"/>), so this is the run's choice, not a scenario's field.
/// </summary>
public enum SplitSelection
{
    Dev,
    [WireName("held_out")] HeldOut,
    All,
}

public static class SplitSelections
{
    /// <summary>The selection as the protocol's first line names it (<c>held_out</c>): the member's own wire name, read by <see cref="SettingChoice"/> and by the JSON writer alike (F18 step 7.5.2).</summary>
    public static string WireName(SplitSelection selection) => SettingChoice.Wire(selection);

    /// <summary>The selection a first line names (<see cref="WireName"/>); the default when it names none this build knows, as a run that predates the split did.</summary>
    public static SplitSelection FromWireName(string? name) =>
        SettingChoice.TryParse(name, out SplitSelection? selection) && selection is { } known ? known : SplitSelection.Dev;

    /// <summary>Whether a run with this selection runs a scenario of the split.</summary>
    public static bool Includes(this SplitSelection selection, DatasetSplit split) => selection switch
    {
        SplitSelection.All => true,
        SplitSelection.HeldOut => split == DatasetSplit.HeldOut,
        _ => split == DatasetSplit.Dev,
    };
}
