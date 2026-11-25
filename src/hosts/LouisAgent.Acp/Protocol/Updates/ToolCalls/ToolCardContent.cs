using System.Text.Json.Serialization;

namespace LouisAgent.Acp.Protocol.Updates.ToolCalls;

/// <summary>Something a tool call's card shows; we only show text, a preview of the result.</summary>
internal sealed record ToolCardContent(TextBlock Content)
{
    /// <summary>The kind of thing shown: plain content, rather than a diff or a terminal.</summary>
    [JsonPropertyOrder(-1)]
    public string Type => "content";
}
