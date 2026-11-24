namespace LouisAgent.Core.Agent.Messages.Holes.Calls;

/// <summary>A call cut off by the output limit, never run, and that limit.</summary>
public sealed record ToolCallSkippedHoles(string Tool, int MaxOutputTokens);
