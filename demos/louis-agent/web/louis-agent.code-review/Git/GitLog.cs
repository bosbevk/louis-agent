namespace LouisAgent.CodeReview.Git;

/// <summary>The commit log as the API lists it.</summary>
public sealed record GitLog(List<GitCommit> Commits);
