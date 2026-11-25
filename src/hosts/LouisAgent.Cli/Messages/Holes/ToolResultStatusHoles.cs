namespace LouisAgent.Cli.Messages.Holes;

/// <summary>How a tool call ended, done or failed, and the first line of what it gave back.</summary>
public sealed record ToolResultStatusHoles(string Outcome, string Result);
