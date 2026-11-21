using System.Text.Json.Serialization;
using LouisAgent.Evals.Protocol.Parts.Conversations;

namespace LouisAgent.Evals.Protocol.Events.Conversations;

/// <summary>The runner put a question to the agent, in a session, on one turn of a conversation.</summary>
public sealed record QuestionEvent(
    DateTimeOffset T,
    string Mode,
    int Repeat,
    string Task,
    int Turn,
    string Session,
    string Text) : EvalEvent(T)
{
    /// <summary>Which turn this question opens.</summary>
    [JsonIgnore]
    public TurnKey Key => new(Mode, Repeat, Task, Turn);
}
