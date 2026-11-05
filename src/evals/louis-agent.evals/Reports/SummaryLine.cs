using LouisAgent.Evals.Protocol.Parts.Run;

namespace LouisAgent.Evals.Reports;

/// <summary>
/// One slice of a mode's trials, rolled up: <c>overall</c>, or one category. <see cref="N"/> is every trial in the
/// slice; the counts say what became of them; the rates are over the counted ones (an infrastructure trial is left
/// out of them) and are null when fewer than <see cref="RunSummary.MinimumForRates"/> trials count
/// (<see cref="TooFew"/>), never 0. Its counts per grader and per kind are the contract's own records.
/// </summary>
public sealed record SummaryLine(
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
    IReadOnlyList<GraderCount> Graders,
    IReadOnlyList<KindCount> Kinds);
