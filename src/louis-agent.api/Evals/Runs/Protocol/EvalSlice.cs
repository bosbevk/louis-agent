using LouisAgent.Api.Evals.Runs.Kinds;

namespace LouisAgent.Api.Evals.Runs.Protocol;

/// <summary>
/// How many of a slice's trials one grader passed, failed or couldn't tell on, kept exactly as the runner counted
/// them.
/// </summary>
public sealed record SliceGrader(string Grader, EvalGraderKind Kind, int Passed, int Failed, int Unknown);

/// <summary>The same counts added up over all the graders of one kind.</summary>
public sealed record SliceKind(EvalGraderKind Kind, int Passed, int Failed, int Unknown);

/// <summary>The average of one numeric score over the trials that have it, and how many that is.</summary>
public sealed record SliceScore(string Key, double Mean, int N);

/// <summary>
/// One slice of a mode's roll-up, its <c>overall</c> line or one category, as the runner's <c>summary</c> line wrote
/// it. We keep the runner's numbers and work out no rate ourselves; a rate is null when the slice had too few trials
/// (<see cref="TooFew"/>), never 0.
/// </summary>
/// <remarks>
/// No cost here on purpose: the ledger prices a slice when it's read (<see cref="Reports.SlicePricer"/>). So an export
/// carries no cost, and grading an export again can't smuggle an old cost into a new run.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0007-measure-every-request.md">ADR 0007: measure every request</seealso>
public sealed record EvalSlice(
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
    IReadOnlyList<SliceGrader> Graders,
    IReadOnlyList<SliceKind> Kinds,
    IReadOnlyList<SliceScore> Scores);
