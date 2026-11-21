namespace LouisAgent.Api.Contracts.Git.Output;

/// <summary>
/// One changed file, with git's two status letters: what's staged and what's in the working tree (a space means
/// unchanged, a question mark untracked).
/// </summary>
public sealed record GitFileStatus(string Path, string? OldPath, string Index, string WorkTree)
{
    /// <summary>git doesn't know the file yet.</summary>
    public bool Untracked => Index == "?";

    /// <summary>Some of its change is staged for the next commit.</summary>
    public bool Staged => Index is not (" " or "?");

    /// <summary>Some of its change isn't staged yet.</summary>
    public bool Unstaged => Untracked || WorkTree != " ";
}
