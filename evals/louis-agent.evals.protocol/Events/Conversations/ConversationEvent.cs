using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Protocol.Parts.Conversations;

namespace LouisAgent.Evals.Protocol.Events.Conversations;

/// <summary>
/// A conversation finished: who answered, how many errors and seconds it took, every grade, how it came out, and what
/// it measured. The grades decide the outcome; the metrics never do.
/// </summary>
public sealed record ConversationEvent(
    DateTimeOffset T,
    string Mode,
    int Repeat,
    string Task,
    string? Role,
    int Errors,
    double Seconds,
    IReadOnlyList<GradeInfo> Grades,
    TrialOutcome Outcome,
    Metrics Metrics) : EvalEvent(T);
