namespace LouisAgent.CodeReview.Git;

public sealed record GitBranches(string? Current, List<GitBranch> Branches, string? Message);
