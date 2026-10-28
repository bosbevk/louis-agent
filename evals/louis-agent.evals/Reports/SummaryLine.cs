using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Protocol.Kinds;

namespace LouisAgent.Evals.Reports;

/// <summary>How many of a slice's counted trials one grader passed, failed, and could not tell on (a trial it gave no grade counts as Unknown).</summary>
public sealed record GraderCount(string Grader, GraderKind Kind, int Passed, int Failed, int Unknown);

/// <summary>The same counts summed over the required graders of one kind.</summary>
public sealed record KindCount(GraderKind Kind, int Passed, int Failed, int Unknown);

/// <summary>The mean of one numeric score over the trials that have it, and how many that is.</summary>
public sealed record ScoreMean(string Key, double Mean, int N);

/// <summary>
/// One slice of a mode's trials, rolled up (F18 step 5c): <c>overall</c>, or one category. <see cref="N"/> is every trial in the slice;
/// the counts say what became of them; the rates are over the counted ones (an infrastructure trial is left out of them, as in 5a) and are
/// null when fewer than <see cref="RunSummary.MinimumForRates"/> trials count (<see cref="TooFew"/>), never 0.
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
    IReadOnlyList<KindCount> Kinds,
    IReadOnlyList<ScoreMean> Scores);
