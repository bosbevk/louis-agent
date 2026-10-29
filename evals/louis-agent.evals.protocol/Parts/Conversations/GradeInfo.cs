using LouisAgent.Evals.Protocol.Kinds;

namespace LouisAgent.Evals.Protocol.Parts.Conversations;

/// <summary>
/// What one grader made of a conversation: a score from 0 to 1 (partial credit), the outcome and why. When a model
/// judged it, which model and which version of its prompt.
/// </summary>
public sealed record GradeInfo(
    string Grader,
    int Version,
    double Score,
    GradeOutcome Outcome,
    string Reason,
    string? JudgeModel,
    string? JudgePromptHash);
