namespace LouisAgent.Core.Agent.Messages.Holes.Summaries;

/// <summary>A tool's answer too long for the model, and its length, written out.</summary>
public sealed record ToolResultSummarisingHoles(string Tool, string Chars);
