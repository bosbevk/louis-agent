namespace LouisAgent.Core.Agent.Messages.Holes.Calls;

/// <summary>What a tool answered, as the log may show it.</summary>
public sealed record ToolCallResultHoles(string Tool, string Result);
