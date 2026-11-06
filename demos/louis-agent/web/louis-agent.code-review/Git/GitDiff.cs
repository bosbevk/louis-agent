namespace LouisAgent.CodeReview.Git;

/// <summary>One file's changes, staged or not, which the API may have cut short.</summary>
public sealed record GitDiff(string Path, bool Staged, bool Untracked, string Diff, bool Truncated);
