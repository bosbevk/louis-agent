namespace LouisAgent.Orchestrator.Messages.Holes.Checks;

/// <summary>The error handed to people, the team it went to, and why.</summary>
public sealed record EscalatedHoles(string ErrorId, string Team, string Reason);
