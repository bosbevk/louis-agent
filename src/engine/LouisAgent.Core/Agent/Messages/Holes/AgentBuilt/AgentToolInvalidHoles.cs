namespace LouisAgent.Core.Agent.Messages.Holes.AgentBuilt;

/// <summary>An agent-built tool's file that doesn't read, and what is wrong with it.</summary>
public sealed record AgentToolInvalidHoles(string File, string Errors);
