using System.Text.Json;
using LouisAgent.SessionStream;

namespace LouisAgent.Web.Shared.Tests;

/// <summary>Stream events built from objects, for the tests that need a tool result with a given text (FX22), so no JSON is written by hand.</summary>
internal static class StreamEvents
{
    private static JsonElement Json(object data) => JsonSerializer.SerializeToElement(data);

    public static SessionEvent ToolUse(string id, string name, object? input = null) =>
        new ContentBlockStarted(Json(new { type = "content_block_start", index = 0, content_block = new { type = "tool_use", id, name, input = input ?? new { } } }));

    public static SessionEvent ToolResult(string id, string content, bool failed = false) =>
        new ContentBlockStarted(Json(new { type = "content_block_start", index = 1, content_block = new { type = "tool_result", tool_use_id = id, content, is_error = failed } }));

    public static SessionEvent TextStart() =>
        new ContentBlockStarted(Json(new { type = "content_block_start", index = 2, content_block = new { type = "text", text = "" } }));

    public static SessionEvent TextDelta(string text) =>
        new ContentBlockDelta(Json(new { type = "content_block_delta", index = 2, delta = new { type = "text_delta", text } }));

    /// <summary>An event of the sub-session a delegation call opened, as the API wraps it (F5).</summary>
    public static SessionEvent InSubSession(string callId, SessionEvent inner) =>
        new SubSessionStreamed(Json(new { type = "sub_session", tool_use_id = callId, agent = "puls", @event = inner.Type, data = inner.Data }), callId, inner);
}
