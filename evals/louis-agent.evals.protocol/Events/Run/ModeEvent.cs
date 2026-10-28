namespace LouisAgent.Evals.Protocol.Events.Run;

/// <summary>
/// A mode starts one repeat: the toolsets, role and model its sessions open with, and how many conversations it will
/// run.
/// </summary>
public sealed record ModeEvent(
    DateTimeOffset T,
    string Mode,
    int Repeat,
    IReadOnlyList<string> Toolsets,
    string? Role,
    string? Llm,
    int? Conversations) : EvalEvent(T);
