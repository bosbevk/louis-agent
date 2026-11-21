namespace LouisAgent.Packs.Git.Web.Changes;

/// <summary>One file's changes, staged or not, which the API may have cut short.</summary>
public sealed record GitDiff(string Path, bool Staged, bool Untracked, string Diff, bool Truncated);
