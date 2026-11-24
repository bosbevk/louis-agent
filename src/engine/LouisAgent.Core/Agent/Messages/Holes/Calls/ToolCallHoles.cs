namespace LouisAgent.Core.Agent.Messages.Holes.Calls;

/// <summary>The tool the model called, and its arguments as the log may show them.</summary>
public sealed record ToolCallHoles(string Tool, string Arguments);
