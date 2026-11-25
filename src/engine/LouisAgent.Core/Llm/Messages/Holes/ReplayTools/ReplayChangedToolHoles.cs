namespace LouisAgent.Core.Llm.Messages.Holes.ReplayTools;

/// <summary>A tool whose name, description or schema changed since the request was recorded.</summary>
public sealed record ReplayChangedToolHoles(string Name);
