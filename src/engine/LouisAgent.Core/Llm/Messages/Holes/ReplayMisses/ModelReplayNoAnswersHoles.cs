namespace LouisAgent.Core.Llm.Messages.Holes.ReplayMisses;

/// <summary>The trial that has no recorded model answers at all.</summary>
public sealed record ModelReplayNoAnswersHoles(string Trial);
