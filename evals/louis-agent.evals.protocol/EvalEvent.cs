using System.Text.Json.Serialization;
using LouisAgent.Evals.Protocol.Events.Conversations;
using LouisAgent.Evals.Protocol.Events.Run;

namespace LouisAgent.Evals.Protocol;

/// <summary>
/// One line the eval runner prints for the API: which event it is, and when it happened. Every event of the protocol
/// is one of the records below.
/// </summary>
/// <remarks>
/// A closed set of cases (ADR 0040) read and written through System.Text.Json's polymorphism: the <c>event</c> member
/// is the type discriminator, so the serializer writes it first and picks the right record when it reads a line, and
/// no code anywhere switches on the event's name. One record per event is what makes the runner and the API share a
/// contract instead of two hand-written copies (CL22). The constructor is <c>private protected</c>, so no case can be
/// added from outside this project.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "event")]
[JsonDerivedType(typeof(ProtocolEvent), "protocol")]
[JsonDerivedType(typeof(ModeEvent), "mode")]
[JsonDerivedType(typeof(QuestionEvent), "question")]
[JsonDerivedType(typeof(SseEvent), "sse")]
[JsonDerivedType(typeof(TurnEvent), "turn")]
[JsonDerivedType(typeof(ConversationEvent), "conversation")]
[JsonDerivedType(typeof(ModeDoneEvent), "mode-done")]
[JsonDerivedType(typeof(SummaryEvent), "summary")]
public abstract record EvalEvent
{
    /// <summary>Only the events of this project can be built.</summary>
    private protected EvalEvent(DateTimeOffset t) => T = t;

    /// <summary>When the runner wrote the line, in UTC, written right after the event's name.</summary>
    [JsonPropertyOrder(-1)]
    public DateTimeOffset T { get; init; }
}
