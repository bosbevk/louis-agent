namespace LouisAgent.Core.Agent.Messages.Holes.Discovery;

/// <summary>A tool found outside the engine that a model without tools can't take.</summary>
public sealed record McpCannotRegisterHoles(string Name);
