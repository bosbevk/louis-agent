using System.Text.Json;

namespace LouisAgent.SessionStream;

/// <summary>Maps an SSE item to its record. The JSON is cloned out of its document, so each event outlives the parse.</summary>
internal static class SessionEventMapper
{
    public static SessionEvent Map(string type, string data)
    {
        using var document = JsonDocument.Parse(data);
        return Map(type, document.RootElement.Clone());
    }

    public static SessionEvent Map(string type, JsonElement data) => type switch
    {
        "message_start" => new MessageStarted(data),
        "content_block_start" => new ContentBlockStarted(data),
        "content_block_delta" => new ContentBlockDelta(data),
        "content_block_stop" => new ContentBlockStopped(data),
        "message_delta" => new MessageDelta(data),
        "message_stop" => new MessageStopped(data),
        "error" => new ErrorReported(data),
        "sub_session" => MapSubSession(data),
        _ => new UnknownEvent(type, data),
    };

    /// <summary>A specialist's event carries the event it sent in "event" and its JSON in "data"; the inner one is mapped too.</summary>
    private static SessionEvent MapSubSession(JsonElement data)
    {
        var toolUseId = data.TryGetProperty("tool_use_id", out var id) ? id.GetString() ?? "" : "";
        var innerType = data.TryGetProperty("event", out var type) ? type.GetString() ?? "" : "";
        var inner = data.TryGetProperty("data", out var innerData)
            ? Map(innerType, innerData)
            : new UnknownEvent(innerType, data);
        return new SubSessionStreamed(data, toolUseId, inner);
    }
}
