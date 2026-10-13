namespace LouisAgent.Web;

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
