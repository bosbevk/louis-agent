namespace LouisAgent.Core.Agent.Messages.Holes.AgentBuilt;

/// <summary>How many agent-built tools were loaded, and from where.</summary>
public sealed record AgentToolsLoadedHoles(int Count, string Directory);
