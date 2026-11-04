using LouisAgent.Evals.Protocol.Kinds;

namespace LouisAgent.Evals.Protocol.Parts.Run;

/// <summary>
/// How one grader graded a slice of a mode: how many passed, failed or couldn't tell, and its mean score, which shows
/// partial credit a pass or a fail hides.
/// </summary>
public sealed record GraderCount(
    string Grader,
    CheckKind Kind,
    int Passed,
    int Failed,
    int Unknown,
    double? ScoreMean);
