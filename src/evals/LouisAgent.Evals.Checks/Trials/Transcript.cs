namespace LouisAgent.Evals.Trials;

/// <summary>The turns of one trial, in order.</summary>
public sealed record Transcript(IReadOnlyList<TurnTranscript> Turns);
