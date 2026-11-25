namespace LouisAgent.Core.Llm.Messages.Holes.ReplayMisses;

/// <summary>The trial whose recordings have no answer for a request, and what in the request changed.</summary>
public sealed record ModelReplayMissHoles(string Trial, string Change);
