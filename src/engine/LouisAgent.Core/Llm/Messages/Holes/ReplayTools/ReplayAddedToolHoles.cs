namespace LouisAgent.Core.Llm.Messages.Holes.ReplayTools;

/// <summary>A tool the request sends that the recording didn't have.</summary>
public sealed record ReplayAddedToolHoles(string Name);
