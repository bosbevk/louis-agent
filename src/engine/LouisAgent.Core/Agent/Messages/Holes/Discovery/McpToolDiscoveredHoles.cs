namespace LouisAgent.Core.Agent.Messages.Holes.Discovery;

/// <summary>A tool found outside the engine, and what it says it does.</summary>
public sealed record McpToolDiscoveredHoles(string Name, string Description);
