using System.Text.Json;
using System.Text.Json.Serialization;
using LouisAgent.Evals.Protocol.Parts.Conversations;

namespace LouisAgent.Evals.Protocol.Events.Conversations;

/// <summary>
/// One event of a turn's stream, passed on as the session sent it: its type and its data, kept as the JSON it was so
/// the portal can replay the turn.
/// </summary>
/// <remarks>
/// The data stays a <see cref="JsonElement"/>: it is the session stream's shape, not ours, and ADR 0041 keeps the
/// read-only DOM for input we don't own (finding 26 gives the stream its own records later).
/// </remarks>
public sealed record SseEvent(
    DateTimeOffset T,
    string Mode,
    int Repeat,
    string Task,
    int Turn,
    string Type,
    JsonElement Data) : EvalEvent(T)
{
    /// <summary>Which turn this stream event belongs to.</summary>
    [JsonIgnore]
    public TurnKey Key => new(Mode, Repeat, Task, Turn);
}
