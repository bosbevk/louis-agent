namespace LouisAgent.CodeReview.Git;

/// <summary>One commit opened: its message and its diff, which the API may have cut short.</summary>
public sealed record GitCommitDetail(GitCommit Commit, string Body, string Diff, bool Truncated);
