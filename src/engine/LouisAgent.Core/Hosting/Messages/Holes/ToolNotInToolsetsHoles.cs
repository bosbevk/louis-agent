namespace LouisAgent.Core.Hosting.Messages.Holes;

/// <summary>A tool a session named that none of its toolsets has, and the toolsets it has.</summary>
public sealed record ToolNotInToolsetsHoles(string Tool, string Toolsets);
