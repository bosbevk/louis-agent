namespace LouisAgent.Core.Agent.Messages.Holes.Replay;

/// <summary>The tool a replay was asked to run with arguments its recording never saw, and the trial it read.</summary>
public sealed record ToolReplayMissHoles(string Tool, string Trial);
