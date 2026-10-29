using System.Text.Json;
using LouisAgent.Choices;

namespace LouisAgent.SessionStream;

/// <summary>One event of a session's stream: its SSE event name as sent, and its JSON as sent.</summary>
public abstract record SessionEvent(string Type, JsonElement Data)
{
    /// <summary>
    /// An event from a type and its JSON, mapped as the reader maps one: for an event kept as data (a stored run's
    /// lines) rather than read from a stream.
    /// </summary>
    public static SessionEvent From(string type, JsonElement data) => SessionEventMapper.Map(type, data);
}

/// <summary>
/// The turn's start: the toolsets the session opened with and the role it runs as, read from the message's toolsets
/// and session_role.
/// </summary>
public sealed record MessageStarted(JsonElement Data, IReadOnlyList<string> Toolsets, string? Role)
    : SessionEvent("message_start", Data);

/// <summary>A block of the answer begins: text, thinking, or a tool call.</summary>
public sealed record ContentBlockStarted(JsonElement Data) : SessionEvent("content_block_start", Data);

/// <summary>A piece of the open block: some text, some thinking, or part of a tool call's input.</summary>
public sealed record ContentBlockDelta(JsonElement Data) : SessionEvent("content_block_delta", Data);

/// <summary>The open block is done.</summary>
public sealed record ContentBlockStopped(JsonElement Data) : SessionEvent("content_block_stop", Data);

/// <summary>The turn's end in numbers: why it stopped, and what it used.</summary>
public sealed record MessageDelta(JsonElement Data) : SessionEvent("message_delta", Data);

/// <summary>The turn is over.</summary>
public sealed record MessageStopped(JsonElement Data) : SessionEvent("message_stop", Data);

/// <summary>A specialist's event, nested: <see cref="Inner"/> is what the specialist sent.</summary>
public sealed record SubSessionStreamed(JsonElement Data, string ToolUseId, SessionEvent Inner)
    : SessionEvent("sub_session", Data);

/// <summary>
/// A failed turn's error: the message, and what kind of failure it was (FX26); no kind when the event names none this
/// reader knows.
/// </summary>
public sealed record ErrorReported(JsonElement Data) : SessionEvent("error", Data)
{
    /// <summary>
    /// The failure's kind, read once from <c>error.type</c>; none for a type this reader doesn't know.
    /// </summary>
    public StreamErrorKind? Kind => KindOf(Data);

    /// <summary>The kind an error event's data names, if it names one this reader knows.</summary>
    /// <remarks>Guard clauses: each shape that names no kind returns first, so the parse is the last step.</remarks>
    private static StreamErrorKind? KindOf(JsonElement data)
    {
        if (data.ValueKind != JsonValueKind.Object) return null;
        if (!data.TryGetProperty("error", out var error) || error.ValueKind != JsonValueKind.Object) return null;
        if (!error.TryGetProperty("type", out var type) || type.ValueKind != JsonValueKind.String) return null;

        return SettingChoice.TryParse(type.GetString(), out StreamErrorKind? kind) ? kind : null;
    }
}

/// <summary>A type the reader doesn't know yet: kept, never dropped.</summary>
public sealed record UnknownEvent(string EventType, JsonElement Data) : SessionEvent(EventType, Data);
