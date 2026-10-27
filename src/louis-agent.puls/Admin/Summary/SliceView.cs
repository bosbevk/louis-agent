namespace LouisAgent.Puls.Admin.Summary;

/// <summary>
/// One slice of a mode's roll-up as the run's JSON carries it (F18 step 5c.3): the runner's counts and rates and the ledger's costs, read and shown, never worked out here.
/// A rate the runner left out (<see cref="TooFew"/>) is null; a cost per trial that is null is unknown; <see cref="HasCostPerPass"/> is false when nothing passed (the API leaves the cost per pass out when it is null, so a pass with no cost is an unknown one).
/// </summary>
public sealed record SliceView(
    string Mode,
    string Slice,
    int N,
    int Passed,
    int Failed,
    int Unknown,
    int Infrastructure,
    IReadOnlyList<string> Tasks,
    bool TooFew,
    double? PassRate,
    double? PassAtK,
    double? PassHatK,
    decimal? CostPerTrial,
    bool HasCostPerPass,
    decimal? CostPerPass);

/// <summary>One mode's slices, the overall line first.</summary>
public sealed record ModeSlices(string Mode, IReadOnlyList<SliceView> Slices);
