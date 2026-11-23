namespace LouisAgent.Api.Evals.Messages.Holes;

/// <summary>The run that failed, and why.</summary>
public sealed record RunFailedHoles(string Id, string Reason);
