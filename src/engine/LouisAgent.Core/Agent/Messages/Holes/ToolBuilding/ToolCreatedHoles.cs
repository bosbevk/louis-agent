namespace LouisAgent.Core.Agent.Messages.Holes.ToolBuilding;

/// <summary>A tool the model made: its name, the language it runs in, and its parameters in a few words.</summary>
public sealed record ToolCreatedHoles(string Name, string Language, string Parameters);
