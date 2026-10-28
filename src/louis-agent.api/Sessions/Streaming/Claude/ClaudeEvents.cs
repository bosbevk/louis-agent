using System.Text.Json;
using System.Text.Json.Serialization;
using LouisAgent.Api.Contracts;

namespace LouisAgent.Api.Sessions.Streaming.Claude;

/// <summary>
/// The turn's start: who is answering, with the toolsets the session loaded and the role it runs as.
/// </summary>
/// <remarks>
/// Data Transfer Object (Fowler, PoEAA): each event the Claude format writes is a named record, so its JSON is a
/// shape a test can name and a reader can share, where an anonymous object was neither. Every event puts its
/// <c>type</c> first, as Claude's own stream does.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
internal sealed record MessageStartEvent(MessageStartBody Message)
{
    /// <summary>The event's name inside its data, as Claude writes it.</summary>
    [JsonPropertyOrder(-1)]
    public string Type => "message_start";
}

/// <summary>The message a turn starts: its session, the speaker, and what the session loaded.</summary>
internal sealed record MessageStartBody(
    string SessionId,
    string Role,
    IReadOnlyList<string> Toolsets,
    string? SessionRole);

/// <summary>A specialist's own event, nested under the call that opened its session (F5).</summary>
internal sealed record SubSessionEventData(string? ToolUseId, string Agent, string Event, JsonElement Data)
{
    /// <summary>The event's name inside its data.</summary>
    [JsonPropertyOrder(-1)]
    public string Type => "sub_session";
}

/// <summary>Why the turn stopped, and what it used when that was recorded.</summary>
internal sealed record MessageDeltaEvent(
    StopDelta Delta,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    object? Usage)
{
    /// <summary>The event's name inside its data.</summary>
    [JsonPropertyOrder(-1)]
    public string Type => "message_delta";
}

/// <summary>The reason a turn stopped (the end of its answer, a limit, a cancel).</summary>
internal sealed record StopDelta(string StopReason);

/// <summary>The turn is over.</summary>
internal sealed record MessageStopEvent
{
    /// <summary>The event's name inside its data.</summary>
    public string Type => "message_stop";
}

/// <summary>
/// A failed turn's error with what the turn used before it failed. Claude's error event has no usage; its clients
/// ignore a field they don't know.
/// </summary>
internal sealed record StreamErrorEvent(ApiErrorDetail Error, object Usage)
{
    /// <summary>The event's name inside its data.</summary>
    [JsonPropertyOrder(-1)]
    public string Type => "error";
}
