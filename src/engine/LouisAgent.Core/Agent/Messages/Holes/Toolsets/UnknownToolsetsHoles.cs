namespace LouisAgent.Core.Agent.Messages.Holes.Toolsets;

/// <summary>The toolsets asked for that don't exist, and the ones that do.</summary>
public sealed record UnknownToolsetsHoles(string Names, string Valid);
