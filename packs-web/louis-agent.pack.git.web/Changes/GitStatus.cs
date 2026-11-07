namespace LouisAgent.Packs.Git.Web.Changes;

/// <summary>What's changed in the workspace: the branch, its changed files, and what a commit just made.</summary>
public sealed record GitStatus(string? Branch, List<GitFile> Files, string? Committed);
