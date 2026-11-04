namespace LouisAgent.Web.Git;

public sealed record GitStatus(string? Branch, List<GitFile> Files, string? Committed);
