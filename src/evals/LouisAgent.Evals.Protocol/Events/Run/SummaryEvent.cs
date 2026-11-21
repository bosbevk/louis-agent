using LouisAgent.Evals.Protocol.Parts.Run;

namespace LouisAgent.Evals.Protocol.Events.Run;

/// <summary>
/// One slice of a mode's results (overall, or one category such as <c>budget</c>), written after the mode's last
/// repeat: how many trials it had and how they came out, its tasks, its rates (null when it had too few trials for
/// one), and the counts per grader and per kind of grader. The cost isn't here: the API prices a slice from the
/// ledger.
/// </summary>
public sealed record SummaryEvent(
    DateTimeOffset T,
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
    IReadOnlyList<KindCount> Kinds) : EvalEvent(T);
