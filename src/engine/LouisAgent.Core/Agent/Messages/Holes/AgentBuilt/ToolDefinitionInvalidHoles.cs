namespace LouisAgent.Core.Agent.Messages.Holes.AgentBuilt;

/// <summary>What is wrong with a tool definition the model wrote, one problem a line.</summary>
public sealed record ToolDefinitionInvalidHoles(string Errors);
