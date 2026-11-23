namespace LouisAgent.Evals.Messages.Holes.Catalog;

/// <summary>The file, and the agent it names that the host doesn't have.</summary>
public sealed record AgentUnknownHoles(string Path, string Agent);
