using System.Text.Json;

namespace LouisAgent.SessionStream;

/// <summary>One event of a session's stream: its SSE event name as sent, and its JSON as sent.</summary>
public abstract record SessionEvent(string Type, JsonElement Data)
{
    /// <summary>An event from a type and its JSON, mapped as the reader maps one: for an event kept as data (a stored run's
    /// lines) rather than read from a stream.</summary>
    public static SessionEvent From(string type, JsonElement data) => SessionEventMapper.Map(type, data);
}
/// <summary>The turn's start: the toolsets the session opened with and the role it runs as, read from the message's toolsets and session_role.</summary>
public sealed record MessageStarted(JsonElement Data, IReadOnlyList<string> Toolsets, string? Role) : SessionEvent("message_start", Data);

public sealed record ContentBlockStarted(JsonElement Data) : SessionEvent("content_block_start", Data);

public sealed record ContentBlockDelta(JsonElement Data) : SessionEvent("content_block_delta", Data);

public sealed record ContentBlockStopped(JsonElement Data) : SessionEvent("content_block_stop", Data);

public sealed record MessageDelta(JsonElement Data) : SessionEvent("message_delta", Data);

public sealed record MessageStopped(JsonElement Data) : SessionEvent("message_stop", Data);

/// <summary>A specialist's event, nested: <see cref="Inner"/> is what the specialist sent.</summary>
public sealed record SubSessionStreamed(JsonElement Data, string ToolUseId, SessionEvent Inner) : SessionEvent("sub_session", Data);

public sealed record ErrorReported(JsonElement Data) : SessionEvent("error", Data);

/// <summary>A type the reader doesn't know yet: kept, never dropped.</summary>
public sealed record UnknownEvent(string EventType, JsonElement Data) : SessionEvent(EventType, Data);
