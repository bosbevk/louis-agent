namespace LouisAgent.Core.Agent.Messages.Holes.ToolBuilding;

/// <summary>An agent-built tool waiting for approval, and what it does.</summary>
public sealed record AgentToolPendingHoles(string Name, string Description);
