using System.Text.Json.Serialization;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Protocol.Parts.Conversations;

namespace LouisAgent.Evals.Protocol.Events.Conversations;

/// <summary>
/// A turn finished: the question and the answer, who answered, which specialist it continued, the tool calls and the
/// sites it read, how long it took, the agent's error if it reported one, and what on our side broke if something
/// ended it early.
/// </summary>
public sealed record TurnEvent(
    DateTimeOffset T,
    string Mode,
    int Repeat,
    string Task,
    int Turn,
    string Session,
    string Question,
    string? Answer,
    string? Role,
    string? Continued,
    int AgentCalls,
    int SpecialistCalls,
    IReadOnlyList<string> Hosts,
    double Seconds,
    string? Error,
    TurnFailure? Failure) : EvalEvent(T)
{
    /// <summary>Which turn this is.</summary>
    [JsonIgnore]
    public TurnKey Key => new(Mode, Repeat, Task, Turn);
}
