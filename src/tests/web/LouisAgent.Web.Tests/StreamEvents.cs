using System.Text.Json;
using LouisAgent.SessionStream;
using LouisAgent.Testing;

namespace LouisAgent.Web.Tests;

/// <summary>Stream events built from objects, for the tests that need a tool result with a given text.</summary>
internal static class StreamEvents
{
    /// <summary>An object as the JSON an event carries.</summary>
    private static JsonElement Json(object data) => JsonSerializer.SerializeToElement(data);

    /// <summary>A tool call starting.</summary>
    public static SessionEvent ToolUse(string id, string name, object? input = null) =>
        new ContentBlockStarted(Json(new
        {
            type = "content_block_start",
            index = 0,
            content_block = new { type = "tool_use", id, name, input = input ?? new { } }
        }));

    /// <summary>A tool call's result, or its failure.</summary>
    public static SessionEvent ToolResult(string id, string content, bool failed = false) =>
        new ContentBlockStarted(Json(new
        {
            type = "content_block_start",
            index = 1,
            content_block = new { type = "tool_result", tool_use_id = id, content, is_error = failed }
        }));

    /// <summary>A text block starting.</summary>
    public static SessionEvent TextStart() =>
        new ContentBlockStarted(Json(new
        { type = "content_block_start", index = 2, content_block = new { type = "text", text = "" } }));

    /// <summary>More words of the text.</summary>
    public static SessionEvent TextDelta(string text) =>
        new ContentBlockDelta(Json(new
        { type = "content_block_delta", index = 2, delta = new { type = "text_delta", text } }));

    /// <summary>An event of the sub-session a delegation call opened, as the API wraps it (F5).</summary>
    public static SessionEvent InSubSession(string callId, SessionEvent inner) =>
        new SubSessionStreamed(
            Json(new
            {
                type = "sub_session",
                tool_use_id = callId,
                agent = SampleIdentity.Alpha,
                @event = inner.Type,
                data = inner.Data
            }), callId, inner);
}
