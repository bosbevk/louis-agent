namespace LouisAgent.Core.Llm.Messages.Holes.ReplayMessages;

/// <summary>Which message of the request changed since it was recorded, counted from 1, and whose it is.</summary>
public sealed record ReplayChangedMessageHoles(int Number, string Role);
