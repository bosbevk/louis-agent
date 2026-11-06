namespace LouisAgent.CodeReview.Git;

public sealed record GitStatus(string? Branch, List<GitFile> Files, string? Committed);
