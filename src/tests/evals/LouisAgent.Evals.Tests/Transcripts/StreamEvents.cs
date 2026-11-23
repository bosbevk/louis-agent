using System.Text.Json;
using LouisAgent.Choices;
using LouisAgent.SessionStream;

namespace LouisAgent.Evals.Tests.Transcripts;

/// <summary>
/// A turn's events written by hand, each in the shape the API's stream writes it (<c>ClaudeStreamFormat</c>), so
/// the transcript's tests read what a real turn sends.
/// </summary>
internal static class StreamEvents
{
    /// <summary>The turn starts, for one session.</summary>
    public static SessionEvent Started() =>
        Event("message_start",
            """{"type":"message_start","message":{"session_id":"s1","role":"assistant","toolsets":[]}}""");

    /// <summary>A block of text opens, and the next deltas are its words.</summary>
    public static SessionEvent TextOpened(int index) =>
        Event("content_block_start",
            $$$"""{"type":"content_block_start","index":{{{index}}},"content_block":{"type":"text","text":""}}""");

    /// <summary>A piece of the answer, as the agent sent it.</summary>
    public static SessionEvent Text(string text)
    {
        var delta = new TextDeltaJson(text);
        return Event("content_block_delta",
            $$"""{"type":"content_block_delta","index":0,"delta":{{Json(delta)}}}""");
    }

    /// <summary>A piece of the agent's thinking, which is never its answer.</summary>
    public static SessionEvent Thinking(string thinking) =>
        Event("content_block_delta",
            """{"type":"content_block_delta","index":0,"delta":""" +
            $$$"""{"type":"thinking_delta","thinking":"{{{thinking}}}"}}""");

    /// <summary>A block closes.</summary>
    public static SessionEvent Closed(int index) =>
        Event("content_block_stop", $$"""{"type":"content_block_stop","index":{{index}}}""");

    /// <summary>The agent calls a tool, with its input as a JSON object.</summary>
    public static SessionEvent ToolUse(string id, string name, string input) =>
        Event("content_block_start", ToolUseJson(id, name, input));

    /// <summary>What a tool gave back, and whether it failed.</summary>
    public static SessionEvent ToolResult(string id, string content, bool failed = false)
    {
        var result = new ToolResultJson(id, content, failed);
        return Event("content_block_start",
            $$"""{"type":"content_block_start","index":2,"content_block":{{Json(result)}}}""");
    }

    /// <summary>The turn ends, with its usage; a cost of null is usage nobody could price.</summary>
    public static SessionEvent Stopped(decimal? cost, string? currency)
    {
        var usage = new UsageJson(120, 30, cost, currency);
        return Event("message_delta",
            $$"""{"type":"message_delta","delta":{"stop_reason":"end_turn"},"usage":{{Json(usage)}}}""");
    }

    /// <summary>The turn failed, with its kind and its message, and the usage it had reached.</summary>
    public static SessionEvent Failed(string message, decimal? cost = null, string? currency = null)
    {
        var error = new ErrorJson(StreamErrorKind.Api.Wire(), message);
        var usage = new UsageJson(10, 0, cost, currency);
        return Event("error", $$"""{"type":"error","error":{{Json(error)}},"usage":{{Json(usage)}}}""");
    }

    /// <summary>One event of the agent this turn asked, as the stream nests it under the call that asked it.</summary>
    public static SessionEvent AskedAgents(string callId, string innerType, string innerData) =>
        Event("sub_session",
            $$"""{"type":"sub_session","tool_use_id":"{{callId}}","agent":"triage.sorter","event":"{{innerType}}",""" +
            $$"""
              "data":{{innerData}}}
              """);

    /// <summary>A tool call's block, as its own JSON, for an asked agent's events too.</summary>
    public static string ToolUseJson(string id, string name, string input) =>
        $$"""{"type":"content_block_start","index":1,"content_block":{"type":"tool_use","id":"{{id}}",""" +
        $$$"""
           "name":"{{{name}}}","input":{{{input}}}}}
           """;

    /// <summary>The event as the stream's reader makes it from its type and its data.</summary>
    private static SessionEvent Event(string type, string data)
    {
        using var document = JsonDocument.Parse(data);
        return SessionEvent.From(type, document.RootElement.Clone());
    }

    /// <summary>A value written as the stream writes it, in snake case.</summary>
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, SnakeCase);

    private static readonly JsonSerializerOptions SnakeCase =
        new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    /// <summary>A text delta's body.</summary>
    private sealed record TextDeltaJson(string Text)
    {
        public string Type => "text_delta";
    }

    /// <summary>A tool result's block.</summary>
    private sealed record ToolResultJson(string ToolUseId, string Content, bool IsError)
    {
        public string Type => "tool_result";
    }

    /// <summary>The usage numbers a turn ends with.</summary>
    private sealed record UsageJson(long InputTokens, long OutputTokens, decimal? Cost, string? Currency);

    /// <summary>A failure's kind and message.</summary>
    private sealed record ErrorJson(string Type, string Message);
}
