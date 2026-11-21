namespace LouisAgent.Web.Chats;

/// <summary>A piece of the answer: thinking, text, or a tool call with its result.</summary>
public sealed class Block
{
    public BlockKind Kind { get; init; }
    public string Text { get; set; } = "";

    public double? ThinkingSeconds { get; set; }
    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.Now;

    public string? ToolUseId { get; init; }
    public string? ToolName { get; init; }

    /// <summary>
    /// The session an AskAgent call opened or carried on, from its first event (F127.4.2), so a page's window can take
    /// the conversation over; none for any other call.
    /// </summary>
    public string? SubSessionId { get; set; }

    /// <summary>
    /// Whether a page's window shows this ask's talk (F127.4.2), so the asker's chat says so in one line instead.
    /// </summary>
    public bool InWindow { get; set; }

    public string? ToolInput { get; init; }
    public string? ToolResult { get; set; }
    public bool ToolFailed { get; set; }

    /// <summary>
    /// The images the tool result gave, as <c>![alt](url)</c> URLs, taken when the result arrived (FX22), so the saved
    /// chat's cut of a long result keeps them. None for a failed result and for a delegation call's (that text is a
    /// specialist's answer, not what a tool read).
    /// </summary>
    public List<string> Images { get; set; } = [];

    /// <summary>
    /// For an <c>AskAgent</c> call (F5): the asked agent's own turn (its thinking, tool calls and answer), folded
    /// from the sub-session events the API streams while the call runs. Null for other tools, and for older chats.
    /// </summary>
    public Turn? SubTurn { get; set; }

    /// <summary>The peer a sub-session ran on, from its events (the call may leave the name empty: the only peer).
    /// </summary>
    public string? SubAgent { get; set; }
}
