namespace LouisAgent.Core.Llm.Messages.Holes.ReplayMessages;

/// <summary>A message the recording had that the request no longer sends, counted from 1, and whose it was.</summary>
public sealed record ReplayGoneMessageHoles(int Number, string Role);
