namespace LouisAgent.Evals.Messages.Holes.Judging;

/// <summary>A tool call in a turn: the tool, what it was sent and what it gave back.</summary>
public sealed record ToolCalledHoles(string Tool, string Input, string Result);
