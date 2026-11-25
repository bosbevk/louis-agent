namespace LouisAgent.Core.Llm.Messages.Holes.ReplayMessages;

/// <summary>A message the request has that the recording didn't, counted from 1, and whose it is.</summary>
public sealed record ReplayAddedMessageHoles(int Number, string Role);
