using System.Text.Json.Serialization;

namespace LouisAgent.Api.Sessions.Streaming.Claude;

/// <summary>
/// A content block opens: thinking, text, a tool call or a tool's result, at its place in the answer.
/// </summary>
/// <remarks>
/// Data Transfer Object (Fowler, PoEAA), as the events in <c>ClaudeEvents</c>: a block and its deltas are named
/// records, written in the order Claude writes them, <c>type</c> first.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
internal sealed record ContentBlockStartEvent(int Index, object ContentBlock)
{
    /// <summary>The event's name inside its data.</summary>
    [JsonPropertyOrder(-1)]
    public string Type => "content_block_start";
}

/// <summary>More of the open block: a piece of thinking or of the answer's text.</summary>
internal sealed record ContentBlockDeltaEvent(int Index, object Delta)
{
    /// <summary>The event's name inside its data.</summary>
    [JsonPropertyOrder(-1)]
    public string Type => "content_block_delta";
}

/// <summary>The block at this place is finished.</summary>
internal sealed record ContentBlockStopEvent(int Index)
{
    /// <summary>The event's name inside its data.</summary>
    [JsonPropertyOrder(-1)]
    public string Type => "content_block_stop";
}

/// <summary>A thinking block as it opens, still empty: its text follows as deltas.</summary>
internal sealed record ThinkingBlock
{
    /// <summary>The block's kind.</summary>
    [JsonPropertyOrder(-1)]
    public string Type => "thinking";

    /// <summary>Nothing yet.</summary>
    public string Thinking => "";
}

/// <summary>A text block as it opens, still empty: the answer follows as deltas.</summary>
internal sealed record TextBlock
{
    /// <summary>The block's kind.</summary>
    [JsonPropertyOrder(-1)]
    public string Type => "text";

    /// <summary>Nothing yet.</summary>
    public string Text => "";
}

/// <summary>A piece of the model's thinking.</summary>
internal sealed record ThinkingDelta(string Thinking)
{
    /// <summary>The delta's kind.</summary>
    [JsonPropertyOrder(-1)]
    public string Type => "thinking_delta";
}

/// <summary>A piece of the answer's text.</summary>
internal sealed record TextDelta(string Text)
{
    /// <summary>The delta's kind.</summary>
    [JsonPropertyOrder(-1)]
    public string Type => "text_delta";
}

/// <summary>A tool the agent called, with what it passed.</summary>
internal sealed record ToolUseBlock(string Id, string Name, IDictionary<string, object?> Input)
{
    /// <summary>The block's kind.</summary>
    [JsonPropertyOrder(-1)]
    public string Type => "tool_use";
}

/// <summary>What a tool gave back, and whether that was a failure.</summary>
internal sealed record ToolResultBlock(string ToolUseId, string Content, bool IsError)
{
    /// <summary>The block's kind.</summary>
    [JsonPropertyOrder(-1)]
    public string Type => "tool_result";
}
