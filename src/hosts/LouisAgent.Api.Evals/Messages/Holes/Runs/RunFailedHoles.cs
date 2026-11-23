namespace LouisAgent.Api.Evals.Messages.Holes.Runs;

/// <summary>The run that failed, and why.</summary>
public sealed record RunFailedHoles(string Id, string Reason);
