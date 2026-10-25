namespace LouisAgent.Web.Shared.Chats;

/// <summary>A piece of the answer: thinking, text, or a tool call with its result.</summary>
public sealed class Block
{
    public BlockKind Kind { get; init; }
    public string Text { get; set; } = "";

    public double? ThinkingSeconds { get; set; }
    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.Now;

    public string? ToolUseId { get; init; }
    public string? ToolName { get; init; }
    public string? ToolInput { get; init; }
    public string? ToolResult { get; set; }
    public bool ToolFailed { get; set; }

    /// <summary>
    /// The images the tool result gave, as <c>![alt](url)</c> URLs, taken when the result arrived (FX22), so the saved chat's cut of a long result keeps
    /// them. None for a failed result and for a delegation call's (that text is a specialist's answer, not what a tool read).
    /// </summary>
    public List<string> Images { get; set; } = [];

    /// <summary>
    /// For a <c>StartSubSession</c> call (F5): the specialist's own turn (its thinking, tool calls and answer), folded from
    /// the sub-session events the API streams while the call runs. Null for other tools, and for chats from before.
    /// </summary>
    public Turn? SubTurn { get; set; }

    /// <summary>The peer a sub-session ran on, from its events (the call may leave the name empty: the only peer).</summary>
    public string? SubAgent { get; set; }
}
