namespace LouisAgent.Api.Endpoints.Git.Output;

/// <summary>One changed file. Index/WorkTree are git's X/Y status letters (" " = unchanged, "?" = untracked).</summary>
internal sealed record GitFileStatus(string Path, string? OldPath, string Index, string WorkTree)
{
    public bool Untracked => Index == "?";
    public bool Staged => Index is not (" " or "?");
    public bool Unstaged => Untracked || WorkTree != " ";
}
