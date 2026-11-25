namespace LouisAgent.Orchestrator.Messages.Holes.Startup;

/// <summary>Why louis-agent couldn't be reached at start-up.</summary>
public sealed record LouisAgentUnreachableHoles(string Reason);
