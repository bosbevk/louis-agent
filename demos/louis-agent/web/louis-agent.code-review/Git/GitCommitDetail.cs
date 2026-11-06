namespace LouisAgent.CodeReview.Git;

public sealed record GitCommitDetail(GitCommit Commit, string Body, string Diff, bool Truncated);
