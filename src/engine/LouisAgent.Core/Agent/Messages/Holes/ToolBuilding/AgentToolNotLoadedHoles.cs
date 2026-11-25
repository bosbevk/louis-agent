namespace LouisAgent.Core.Agent.Messages.Holes.ToolBuilding;

/// <summary>A tool waiting on disk from an earlier session, which this one hasn't loaded.</summary>
public sealed record AgentToolNotLoadedHoles(string Name);
