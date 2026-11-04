namespace LouisAgent.Web.Git;

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
