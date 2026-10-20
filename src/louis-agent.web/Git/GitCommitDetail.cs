namespace LouisAgent.Web.Git;

public sealed record GitCommitDetail(GitCommit Commit, string Body, string Diff, bool Truncated);
