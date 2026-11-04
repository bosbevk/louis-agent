namespace LouisAgent.Evals.Protocol.Parts.Conversations;

/// <summary>
/// What a conversation measured: the agent's and the specialists' tool calls, the sites it read, its turns, its tokens
/// in and out, its seconds and its cost. Measured, never graded, so none of them fails a run. Each is null when it
/// isn't known, never 0.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0007-measure-every-request.md">ADR 0007: measure every request</seealso>
public sealed record Metrics(
    int? AgentCalls,
    int? SpecialistCalls,
    IReadOnlyList<string>? Hosts,
    int? Turns,
    long? TokensIn,
    long? TokensOut,
    double? Seconds,
    decimal? Cost);
