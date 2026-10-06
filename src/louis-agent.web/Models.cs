namespace louis_agent.web;

/// <summary>A chat as the browser keeps it; the server only holds the session (and loses it on restart).</summary>
public sealed class Chat
{
    public required string SessionId { get; init; }
    public string Title { get; set; } = "New chat";
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.Now;
    public List<Turn> Turns { get; init; } = [];

    /// <summary>The API no longer knows this session (it was restarted), so the chat can be read but not continued.</summary>
    public bool Ended { get; set; }
}

/// <summary>One user message and the agent's streamed answer to it.</summary>
public sealed class Turn
{
    public string UserText { get; init; } = "";
    public List<string> AttachmentNames { get; init; } = [];
    public List<Block> Blocks { get; init; } = [];
    public string? StopReason { get; set; }
    public string? Error { get; set; }
}

public enum BlockKind { Thinking, Text, Tool }

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
}

public sealed record Attachment(string Name, string Content);

/// <summary>A file or folder in the agent's workspace; <see cref="Path"/> is workspace-relative with forward slashes.</summary>
public sealed record WorkspaceEntry(string Name, string Path, string Type, long? Size, bool Sensitive)
{
    public bool IsDirectory => Type == "directory";
}

public sealed record WorkspaceFolder(string Path, List<WorkspaceEntry> Entries);

public sealed record WorkspaceFile(string Path, string Name, long Size, string Content);

/// <summary>A changed file from git status. Index/WorkTree are git's X/Y letters (" " unchanged, "?" untracked).</summary>
public sealed record GitFile(string Path, string? OldPath, string Index, string WorkTree)
{
    public bool Untracked => Index == "?";
    public bool Staged => Index is not (" " or "?");
    public bool Unstaged => Untracked || WorkTree != " ";

    public string FileName => Path[(Path.LastIndexOf('/') + 1)..];
    public string Folder => Path.Contains('/') ? Path[..Path.LastIndexOf('/')] : "";

    /// <summary>One-letter badge for the staged or the working-tree side (U = untracked).</summary>
    public string Letter(bool staged) => staged ? Index : Untracked ? "U" : WorkTree;
}

public sealed record GitStatus(string? Branch, List<GitFile> Files, string? Committed);

public sealed record GitDiff(string Path, bool Staged, bool Untracked, string Diff, bool Truncated);
