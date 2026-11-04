namespace LouisAgent.Web.Git;

public sealed record GitDiff(string Path, bool Staged, bool Untracked, string Diff, bool Truncated);
