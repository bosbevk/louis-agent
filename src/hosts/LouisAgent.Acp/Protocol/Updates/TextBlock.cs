using System.Text.Json.Serialization;

namespace LouisAgent.Acp.Protocol.Updates;

/// <summary>Plain text, as ACP wraps it: a content block of type <c>text</c>.</summary>
internal sealed record TextBlock(string Text)
{
    /// <summary>The block's kind; we only send text.</summary>
    [JsonPropertyOrder(-1)]
    public string Type => "text";
}
