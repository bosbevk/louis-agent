namespace LouisAgent.Orchestrator.Messages.Holes.Fixes;

/// <summary>How louis-agent's fix ended, and the commit it made on which branch.</summary>
public sealed record LouisAgentCommittedHoles(string Status, string Commit, string? Branch);
