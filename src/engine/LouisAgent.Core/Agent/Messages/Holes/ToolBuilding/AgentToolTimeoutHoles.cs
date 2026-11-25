namespace LouisAgent.Core.Agent.Messages.Holes.ToolBuilding;

/// <summary>
/// An agent-built tool that ran out of time: its name, its limit in seconds, and the end of what it wrote.
/// </summary>
public sealed record AgentToolTimeoutHoles(string Name, int Seconds, string Output);
