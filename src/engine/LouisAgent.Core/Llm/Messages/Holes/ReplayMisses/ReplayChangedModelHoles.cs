namespace LouisAgent.Core.Llm.Messages.Holes.ReplayMisses;

/// <summary>The model a request was recorded on, and the one it asks for now.</summary>
public sealed record ReplayChangedModelHoles(string? Recorded, string? Requested);
