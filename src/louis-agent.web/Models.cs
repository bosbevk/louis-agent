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

    /// <summary>
    /// The chat's usage: the sum of its turns' (F3). Added up here, not asked from the API, because the browser keeps the
    /// chat after the API's session is gone.
    /// </summary>
    public TurnUsage? Usage() =>
        Turns.Select(t => t.Usage).OfType<TurnUsage>().Aggregate((TurnUsage?)null, (total, usage) => total is null ? usage : total + usage);
}

/// <summary>One user message and the agent's streamed answer to it.</summary>
public sealed class Turn
{
    public string UserText { get; init; } = "";
    public List<string> AttachmentNames { get; init; } = [];
    public List<Block> Blocks { get; init; } = [];
    public string? StopReason { get; set; }
    public string? Error { get; set; }

    /// <summary>What the turn's model requests used, from <c>message_delta.usage</c>; null for commands and older chats.</summary>
    public TurnUsage? Usage { get; set; }
}

/// <summary>
/// <c>message_delta.usage</c> from louis-agent.api (F3): Claude's four token fields summed over a turn's model requests,
/// plus how many requests and what they cost. <see cref="Cost"/> is null when no request was priced ("price unknown").
/// </summary>
public sealed record TurnUsage(
    long InputTokens, long CacheCreationInputTokens, long CacheReadInputTokens, long OutputTokens,
    int Requests, decimal? Cost, string? Currency, int Unpriced)
{
    [System.Text.Json.Serialization.JsonIgnore]
    public long TotalTokens => InputTokens + CacheCreationInputTokens + CacheReadInputTokens + OutputTokens;

    /// <summary>Adds two turns up; a cost is only unknown when neither side has one, as on the server.</summary>
    public static TurnUsage operator +(TurnUsage a, TurnUsage b) => new(
        a.InputTokens + b.InputTokens,
        a.CacheCreationInputTokens + b.CacheCreationInputTokens,
        a.CacheReadInputTokens + b.CacheReadInputTokens,
        a.OutputTokens + b.OutputTokens,
        a.Requests + b.Requests,
        a.Cost is null && b.Cost is null ? null : (a.Cost ?? 0) + (b.Cost ?? 0),
        a.Currency ?? b.Currency,
        a.Unpriced + b.Unpriced);
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

/// <summary>A local branch; Ahead/Behind count commits relative to the checked-out branch (Merged = nothing left to merge).</summary>
public sealed record GitBranch(string Name, string Commit, string Subject, string Author, string Date, bool Current, int Ahead, int Behind, bool Merged);

public sealed record GitBranches(string? Current, List<GitBranch> Branches, string? Message);

public sealed record GitCommit(string Sha, string ShortSha, string Author, string Date, string Subject, List<string> Refs, List<string> Parents)
{
    public bool IsMerge => Parents.Count > 1;
}

public sealed record GitLog(List<GitCommit> Commits);

public sealed record GitCommitDetail(GitCommit Commit, string Body, string Diff, bool Truncated);

/// <summary>What merging a branch into <see cref="Into"/> would bring: its commits and their combined diff.</summary>
public sealed record GitBranchDetail(GitBranch Branch, string? Into, List<GitCommit> Commits, string Diff, bool Truncated);
