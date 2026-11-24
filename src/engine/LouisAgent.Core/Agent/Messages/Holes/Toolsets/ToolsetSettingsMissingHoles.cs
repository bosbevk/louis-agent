namespace LouisAgent.Core.Agent.Messages.Holes.Toolsets;

/// <summary>A toolset this instance can't run, and the settings it needs.</summary>
public sealed record ToolsetSettingsMissingHoles(string Name, string Settings);
