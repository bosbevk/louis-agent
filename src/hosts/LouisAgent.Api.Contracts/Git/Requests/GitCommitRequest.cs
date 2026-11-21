namespace LouisAgent.Api.Contracts.Git.Requests;

/// <summary>The message for a commit of what's staged.</summary>
public sealed record GitCommitRequest(string? Message);
