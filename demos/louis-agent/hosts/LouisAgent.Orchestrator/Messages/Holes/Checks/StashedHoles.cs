namespace LouisAgent.Orchestrator.Messages.Holes.Checks;

/// <summary>The branch whose uncommitted changes were stashed before going back to main.</summary>
public sealed record StashedHoles(string Branch);
