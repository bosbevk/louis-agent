namespace LouisAgent.Core.Agent.Messages.Holes.Calls;

/// <summary>A tool that threw: what kind of failure, and why.</summary>
public sealed record ToolCallFailedHoles(string Tool, string ErrorType, string Reason);
