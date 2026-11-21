namespace LouisAgent.Api.Contracts.Git.Requests;

/// <summary>Which branch to merge into the checked-out one.</summary>
public sealed record GitBranchRequest(string? Branch);
