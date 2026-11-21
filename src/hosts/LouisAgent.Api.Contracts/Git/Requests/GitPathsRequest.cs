namespace LouisAgent.Api.Contracts.Git.Requests;

/// <summary>The files to stage, unstage or discard.</summary>
public sealed record GitPathsRequest(List<string>? Paths);
