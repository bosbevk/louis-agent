namespace LouisAgent.Packs.Git.Web.Branches.Records;

/// <summary>The commit log as the API lists it.</summary>
public sealed record GitLog(List<GitCommit> Commits);
