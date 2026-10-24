using System.Text.Json;

namespace LouisAgent.SessionStream;

/// <summary>Maps an SSE item to its record. The JSON is cloned out of its document, so each event outlives the parse.</summary>
internal static class SessionEventMapper
{
    /// <summary>A data line that isn't JSON (a cut-off connection, say) is kept as an UnknownEvent whose Data is its text,
    /// so the events after it are still read.</summary>
    public static SessionEvent Map(string type, string data)
    {
        try
        {
            using var document = JsonDocument.Parse(data);
            return Map(type, document.RootElement.Clone());
        }
        catch (JsonException)
        {
            return new UnknownEvent(type, JsonSerializer.SerializeToElement(data));
        }
    }

    public static SessionEvent Map(string type, JsonElement data)
    {
        // Every event is a JSON object; a value that isn't one is kept as it came, like any other unknown event.
        if (data.ValueKind != JsonValueKind.Object) return new UnknownEvent(type, data);
        return MapObject(type, data);
    }

    private static SessionEvent MapObject(string type, JsonElement data) => type switch
    {
        "message_start" => MapMessageStarted(data),
        "content_block_start" => new ContentBlockStarted(data),
        "content_block_delta" => new ContentBlockDelta(data),
        "content_block_stop" => new ContentBlockStopped(data),
        "message_delta" => new MessageDelta(data),
        "message_stop" => new MessageStopped(data),
        "error" => new ErrorReported(data),
        "sub_session" => MapSubSession(data),
        _ => new UnknownEvent(type, data),
    };

    /// <summary>The toolsets and the role sit in "message", beside the session's id.</summary>
    private static SessionEvent MapMessageStarted(JsonElement data)
    {
        var message = data.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.Object ? m : default;
        var toolsets = new List<string>();
        if (message.ValueKind == JsonValueKind.Object
            && message.TryGetProperty("toolsets", out var list) && list.ValueKind == JsonValueKind.Array)
        {
            toolsets.AddRange(list.EnumerateArray().Where(t => t.ValueKind == JsonValueKind.String).Select(t => t.GetString()!));
        }
        var role = message.ValueKind == JsonValueKind.Object
            && message.TryGetProperty("session_role", out var r) && r.ValueKind == JsonValueKind.String
            ? r.GetString()
            : null;
        return new MessageStarted(data, toolsets, role);
    }

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
