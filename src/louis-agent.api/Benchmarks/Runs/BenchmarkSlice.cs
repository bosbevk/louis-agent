namespace LouisAgent.Api.Benchmarks.Runs;

/// <summary>How many of a slice's trials one grader passed, failed and could not tell on (F18 step 5c): the runner's count, read and kept as it wrote it.</summary>
public sealed record SliceGrader(string Grader, BenchmarkGraderKind Kind, int Passed, int Failed, int Unknown);

/// <summary>The same counts summed over the graders of one kind.</summary>
public sealed record SliceKind(BenchmarkGraderKind Kind, int Passed, int Failed, int Unknown);

/// <summary>The mean of one numeric score over the trials that have it, and how many that is.</summary>
public sealed record SliceScore(string Key, double Mean, int N);

/// <summary>
/// One slice of a mode's roll-up (F18 step 5c): its <c>overall</c> line or one category, as the runner's <c>summary</c> line wrote it. The API keeps the runner's fields only and
/// computes no rate; the rates are null when the slice had too few trials (<see cref="TooFew"/>), never 0. The cost is not here: the ledger prices a slice when it is read
/// (<see cref="Reports.SlicePricer"/>), so an export carries none, and re-scoring an export can't carry a cost into a new run (ADR 0007).
/// </summary>
public sealed record BenchmarkSlice(string Mode, string Slice, int N, int Passed, int Failed, int Unknown, int Infrastructure, IReadOnlyList<string> Tasks,
    bool TooFew, double? PassRate, double? PassAtK, double? PassHatK, IReadOnlyList<SliceGrader> Graders, IReadOnlyList<SliceKind> Kinds, IReadOnlyList<SliceScore> Scores);
