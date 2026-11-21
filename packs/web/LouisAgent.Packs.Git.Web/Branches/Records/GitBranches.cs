namespace LouisAgent.Packs.Git.Web.Branches.Records;

/// <summary>The branches the API lists, which one is checked out, and what the last merge or delete said.</summary>
public sealed record GitBranches(string? Current, List<GitBranch> Branches, string? Message);
