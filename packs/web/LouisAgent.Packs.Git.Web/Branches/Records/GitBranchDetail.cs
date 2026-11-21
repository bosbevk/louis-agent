namespace LouisAgent.Packs.Git.Web.Branches.Records;

/// <summary>What merging a branch into <see cref="Into"/> would bring: its commits and their combined diff.</summary>
public sealed record GitBranchDetail(GitBranch Branch, string? Into, List<GitCommit> Commits, string Diff, bool Truncated);
